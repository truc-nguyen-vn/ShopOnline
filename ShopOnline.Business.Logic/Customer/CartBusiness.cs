﻿using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ShopOnline.Business.Customer;
using ShopOnline.Core;
using ShopOnline.Core.Exceptions;
using ShopOnline.Core.Models.Client;
using ShopOnline.Core.Models.Enum;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopOnline.Business.Logic.Customer
{
    public class CartBusiness : ICartBusiness
    {
        private const string CART_KEY = "cart";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly MyDbContext _context;
        private ISession _session => _httpContextAccessor.HttpContext.Session;
        public CartBusiness(IHttpContextAccessor httpContextAccessor,
                            MyDbContext context)
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public List<ProductCartModel> GetProductsCart()
        {
            string jsonCart = _session.GetString(CART_KEY);
            if (jsonCart != null)
            {
                return JsonConvert.DeserializeObject<List<ProductCartModel>>(jsonCart);
            }
            return new List<ProductCartModel>();
        }

        public async Task AddProductToCartAsync(int idProduct, int quantity)
        {
            var cart = GetProductsCart();
            var productInCart = cart.Where(x => x.Id == idProduct).FirstOrDefault();

            if (productInCart == null)
            {
                productInCart = await _context.Products.Where(x => x.Id == idProduct && !x.IsDeleted)
                                    .Select(x => new ProductCartModel
                                    {
                                        Id = x.Id,
                                        Name = x.Name,
                                        Quantity = x.Quantity,
                                        Size = x.Size,
                                        IdProductDetail = x.IdProductDetail,
                                    })
                                    .FirstOrDefaultAsync();

                if (productInCart == null)
                    throw new UserFriendlyException(ErrorCode.NotFoundInCart);

                cart.Add(productInCart);
            }

            productInCart.SelectedQuantity = quantity == 0
                                    ? productInCart.SelectedQuantity++
                                    : productInCart.SelectedQuantity + quantity;

            if (productInCart.SelectedQuantity > productInCart.Quantity)
                throw new UserFriendlyException(ErrorCode.OutOfStock);

            SaveCartSession(cart);
        }

        public Task ReduceProductFromCartAsync(int idProduct, int? quantity)
        {
            var cart = GetProductsCart();
            var productInCart = cart.Where(x => x.Id == idProduct).FirstOrDefault();

            if (productInCart == null)
                throw new UserFriendlyException(ErrorCode.NotFoundInCart);

            productInCart.SelectedQuantity = quantity == null
                                    ? productInCart.SelectedQuantity--
                                    : productInCart.SelectedQuantity - (int)quantity;

            if (productInCart.SelectedQuantity < 0)
                productInCart.SelectedQuantity = 0;

            SaveCartSession(cart);
            return Task.CompletedTask;
        }

        public Task RemoveProductFromCartAsync(int idProduct)
        {
            var cart = GetProductsCart();
            var productInCart = cart.Where(x => x.Id == idProduct).FirstOrDefault();

            if (productInCart == null)
                throw new UserFriendlyException(ErrorCode.NotFoundInCart);

            cart.Remove(productInCart);

            SaveCartSession(cart);
            return Task.CompletedTask;
        }

        public Task RemoveAllProductFromCartAsync()
        {
            SaveCartSession(new List<ProductCartModel>());
            return Task.CompletedTask;
        }

        private void ClearCart() => _session.Remove(CART_KEY);

        private void SaveCartSession(List<ProductCartModel> products)
        {
            string jsonCart = JsonConvert.SerializeObject(products);
            _session.SetString(CART_KEY, jsonCart);
        }
    }
}
