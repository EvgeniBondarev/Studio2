﻿using System.ComponentModel.DataAnnotations;

namespace OzonDomains.Models
{
    //TODO: Сделать модель для наследования/интерфейс
    public class AppStatus
    {
        public int Id { get; set; }

        [StringLength(50)]
        [Display(Name = "Статус в отчете")]
        public string? Name { get; set; }
    }
}
