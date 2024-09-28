﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OzonDomains
{
    public enum TransactionType
    {
        [Display(Name = "Заказан поставщику")]
        OrderedToSupplier = 0,

        [Display(Name = "Все")]
        All = 1,
    }
}
