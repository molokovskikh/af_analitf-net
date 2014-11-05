using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using NHibernate.Hql.Ast.ANTLR;

namespace AnalitF.Net.Service.Controllers
{
	public class StatusController : ApiController
	{
		public HttpResponseMessage Get()
		{
			return new HttpResponseMessage(HttpStatusCode.OK) {
				Content = new StringContent(Assembly.GetExecutingAssembly().GetName().Version.ToString())
			};
		}
	}
}