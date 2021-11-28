﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShopOnline.Core.Exceptions
{
    public class ProductTypeExistedException: SystemException
    {
        public ProductTypeExistedException(string productType)
              : base($" Product Type is already ")
        {

        }
    }
}
