using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjetoDC.Scripts.Core.Results
{
    public class SystemResult
    {
        public bool Success { get; set; }
        public string Reason { get; set; }

        public static SystemResult Ok(string reason = "")
        {
            return new SystemResult
            {
                Success = true,
                Reason = reason
            };
        }

        public static SystemResult Fail(string reason)
        {
            return new SystemResult
            {
                Success = false,
                Reason = reason
            };
        }
    }
}
