﻿using FluentValidation;
using ShopOnline.Core.Models.Product;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopOnline.Core.Validators.Product
{
    public class BrandCreateValidator: AbstractValidator<BrandCreate>
    {
        public BrandCreateValidator()
        {
            RuleFor(x => x.BrandName).NotEmpty();
        }
    }
}
