using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteInvest.Entity.Server2
{
	public class ServerResponce<T>
	{
		public T Data { get; set; }
		public string ErrorMessage { get; set; }

		public ServerResponce(T data, string message = "")
		{
			Data = data;
			ErrorMessage = message;
		}
	}
}
