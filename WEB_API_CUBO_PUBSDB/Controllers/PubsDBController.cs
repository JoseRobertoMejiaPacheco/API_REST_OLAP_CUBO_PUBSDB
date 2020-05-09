using Microsoft.AnalysisServices.AdomdClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;
using System.Web.Http.Cors;

namespace WEB_API_CUBO_PUBSDB.Controllers
{
    //Todos los otigenes, todas las cabeceras y todos los verbos de metodos(get, put, post)
    [EnableCors(origins: "*", headers: "", methods: "")]
    [RoutePrefix("analysis/pubs")]
    public class PubsDBController : ApiController
    {
        [HttpGet]
        [Route("Testing")]
        public HttpResponseMessage Testing()
        {
            return Request.CreateResponse(HttpStatusCode.OK, "Prueba de Api Exitosa");
        }

        [HttpGet]
        [Route("GetDataItemsByDimension/{dimension}/{order}/{top}")]
        public HttpResponseMessage Top5(string dimension, int order, int top)
        {

            string orderString = "";
            switch (order)
            {
                case 0:
                    orderString = "ASC";
                    break;
                case 1:
                    orderString = "DESC";
                    break;
                default:
                    orderString = "ASC";
                    break;
            }
            string topString = "";
            if (top > 0)
                topString = top.ToString();
            else
                topString= dimension;

            var mdxQuery = $@" 
WITH SET [TopVentas] AS 
	NONEMPTY(
			ORDER(
				{{	{dimension}.CHILDREN}},
					[Measures].[VENTAS],{orderString}))

SELECT NON EMPTY
{{
    ([Measures].[VENTAS])
}} ON COLUMNS , NON EMPTY
	{{
    HEAD(TopVentas, {topString} )
    }} ON ROWS
FROM [PUBS DW]";
            List<string> dim = new List<string>();
            List<decimal> ventas = new List<decimal>();
            List<dynamic> lstTabla = new List<dynamic>();

            dynamic result = new
            {
                datosDimension = dim,
                datosVenta = ventas,
                datosTabla = lstTabla
            };
            using (AdomdConnection cnn = new AdomdConnection(ConfigurationManager.ConnectionStrings["cubopubs"].ConnectionString))
            {
                cnn.Open();
                using (AdomdCommand cmd = new AdomdCommand(mdxQuery, cnn))
                {
                    //cmd.Parameters.Add("Dimension", valoresDimension);
                    using (AdomdDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            dim.Add(dr.GetString(0));
                            ventas.Add(dr.GetDecimal(1));

                            dynamic objTabla = new
                            {
                                descripcion = dr.GetString(0),
                                valor = dr.GetDecimal(1)
                            };

                            lstTabla.Add(objTabla);
                        }
                        dr.Close();
                    }
                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, (object)result);
        }

        [HttpPost]
        [Route("GetDataPieByDimension/{dimension}/{order}")]
        public HttpResponseMessage GetDataPieByDimension(string dimension, string order, string[] values)
        {


            string elementsToSearch = "";
            foreach (var item in values)
            {
                elementsToSearch += ($@"{dimension}.&[{item}],");
            }
            elementsToSearch = elementsToSearch.Remove(elementsToSearch.Length - 1);
            string MDX_QUERY = $@"
            SELECT 
({{ {elementsToSearch} }}) 
 ON ROWS,{{
 [Measures].[VENTAS]
 }}ON COLUMNS FROM [PUBS DW]


";
            List<string> dim = new List<string>();
            List<decimal> ventas = new List<decimal>();
            List<dynamic> lstTabla = new List<dynamic>();

            dynamic result = new
            {
                datosDimension = dim,
                datosVenta = ventas,
                datosTabla = lstTabla
            };
            using (AdomdConnection cnn = new AdomdConnection(ConfigurationManager.ConnectionStrings["cubopubs"].ConnectionString))
            {
                cnn.Open();
                using (AdomdCommand cmd = new AdomdCommand(MDX_QUERY, cnn))
                {
                    //cmd.Parameters.Add("Dimension", valoresDimension);
                    using (AdomdDataReader dr = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        while (dr.Read())
                        {
                            dim.Add(dr.GetString(0));
                            ventas.Add(dr.GetDecimal(1));

                            dynamic objTabla = new
                            {
                                descripcion = dr.GetString(0),
                                valor = dr.GetDecimal(1)
                            };

                            lstTabla.Add(objTabla);
                        }
                        dr.Close();
                    }
                }
            }
            return Request.CreateResponse(HttpStatusCode.OK, (object)result);
        }
    }
}
