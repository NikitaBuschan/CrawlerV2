using System;
using System.ComponentModel.DataAnnotations;

namespace CrawlerVerifier.Model
{
    public class Contract
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Address { get; set; }

        [Required]
        public int ChainId { get; set; }

        [Required]
        public int ContractType { get; set; }

        [Required]
        public int Company { get; set; }

        [Required]
        public Int32 LastBlockRPC { get; set; }

        [Required]
        public Int32 LastBlockWS { get; set; }

        [Required]
        public Int32 CreationBlock { get; set; }

        public Contract()
        {

        }

        public Contract(string address, int chainId, int contractType, int company)
        {
            Address = address;
            ChainId = chainId;
            ContractType = contractType;
            Company = company;
            LastBlockWS = 0;
            LastBlockRPC = 0;
            CreationBlock = 0;
        }
    }
}
