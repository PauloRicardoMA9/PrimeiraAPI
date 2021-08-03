using AutoMapper;
using ma9.Api.ViewModels;
using ma9.Business.Intefaces;
using ma9.Business.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ma9.Api.Controllers
{
    [Route("api/[controller]")]
    public class ProdutosController : MainController
    {
        private readonly IProdutoRepository _produtoRepository;
        private readonly IProdutoService _produtoService;
        private readonly IMapper _mapper;

        public ProdutosController(INotificador notificador, IProdutoRepository produtoRepository, IProdutoService produtoService, IMapper mapper) : base(notificador)
        {
            _produtoRepository = produtoRepository;
            _produtoService = produtoService;
            _mapper = mapper;
        }

        [HttpGet]
        public async Task<IEnumerable<ProdutoViewModel>> ObterTodos()
        {
            return _mapper.Map<IEnumerable<ProdutoViewModel>>(await _produtoRepository.ObterProdutosFornecedores());
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ProdutoViewModel>> ObterPorId(Guid id)
        {
            var produtoViewModel = await ObterProdutoFornecedor(id);
            if (produtoViewModel == null)
            {
                return NotFound();
            }
            return produtoViewModel;
        }

        [HttpPost]
        public async Task<ActionResult> Adicionar(ProdutoViewModel produtoViewModel)
        {
            if (!ModelState.IsValid)
            {
                return NotificarErroModelInvalida(ModelState);
            }
            var imagemNome = Guid.NewGuid() + "_" + produtoViewModel.Imagem;
            var uploadArquivoSucesso = UploadArquivo(produtoViewModel.ImagemUpload, imagemNome);
            if (!uploadArquivoSucesso)
            {
                return ReturnBadRequest();
            }
            produtoViewModel.Imagem = imagemNome;
            await _produtoService.Adicionar(_mapper.Map<Produto>(produtoViewModel));
            return CreatedAtAction("Adicionar", null);
        }

        [HttpPut("{id:guid}")]
        public async Task<ActionResult> Atualizar(Guid id, ProdutoViewModel produtoViewModel)
        {
            if (!ModelState.IsValid)
            {
                return NotificarErroModelInvalida(ModelState);
            }
            if (id != produtoViewModel.Id)
            {
                NotificarErro("Os ids informados não são iguais!");
                return ReturnBadRequest();
            }
            var produtoAtualizacao = await ObterProduto(id);
            if (produtoAtualizacao == null)
            {
                return NotFound();
            }
            if (string.IsNullOrEmpty(produtoViewModel.Imagem))
            {
                produtoViewModel.Imagem = produtoAtualizacao.Imagem;
            }
            if (produtoViewModel.ImagemUpload != null)
            {
                var imagemNome = Guid.NewGuid() + "_" + produtoViewModel.Imagem;
                if (!UploadArquivo(produtoViewModel.ImagemUpload, imagemNome))
                {
                    return ReturnBadRequest();
                }

                produtoAtualizacao.Imagem = imagemNome;
            }

            produtoAtualizacao.FornecedorId = produtoViewModel.FornecedorId;
            produtoAtualizacao.Nome = produtoViewModel.Nome;
            produtoAtualizacao.Descricao = produtoViewModel.Descricao;
            produtoAtualizacao.Valor = produtoViewModel.Valor;
            produtoAtualizacao.Ativo = produtoViewModel.Ativo;

            await _produtoService.Atualizar(_mapper.Map<Produto>(produtoAtualizacao));

            return NoContent();
        }

        [HttpDelete("{id:guid}")]
        public async Task<ActionResult> Excluir(Guid id)
        {
            var produto = await ObterProdutoFornecedor(id);
            if (produto == null)
            {
                return NotFound();
            }
            await _produtoService.Remover(id);
            return NoContent();
        }

        private async Task<ProdutoViewModel> ObterProdutoFornecedor(Guid id)
        {
            return _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoFornecedor(id));
        }

        private async Task<ProdutoViewModel> ObterProduto(Guid id)
        {
            return _mapper.Map<ProdutoViewModel>(await _produtoRepository.ObterProdutoFornecedor(id));
        }

        private bool UploadArquivo(string arquivo, string imgNome)
        {
            if (string.IsNullOrEmpty(arquivo))
            {
                NotificarErro("Forneça uma imagem para este produto!");
                return false;
            }

            var imageDataByteArray = Convert.FromBase64String(arquivo);

            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imagens", imgNome);

            if (System.IO.File.Exists(filePath))
            {
                NotificarErro("Já existe um arquivo com este nome!");
                return false;
            }

            System.IO.File.WriteAllBytes(filePath, imageDataByteArray);

            return true;
        }
    }
}
