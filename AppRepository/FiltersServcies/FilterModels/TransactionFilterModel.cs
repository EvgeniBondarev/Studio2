﻿using OzonDomains.Models;
using OzonDomains;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OServcies.FiltersServcies.FilterModels;

namespace Servcies.FiltersServcies.FilterModels
{
    public class TransactionFilterModel : ITableFilterModel
    {
        private TransactionType? _type { get; set; }
        public TransactionType? Type
        {
            get { return _type; }
            set
            {
                if (value == TransactionType.All)
                {
                    _type = null;
                }
                else
                {
                    _type = value;
                }
            }
        }
        private string? _createBy;
        public string? CreateBy
        {
            get { return _createBy; }
            set
            {
                if (value == "Все")
                {
                    _createBy = null;
                }
                else
                {
                    _createBy = value;
                }
            }
        }
        public DateTime? CreatedDateTime { get; set; }
    }
}
