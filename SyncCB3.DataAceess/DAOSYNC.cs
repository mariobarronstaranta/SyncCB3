using Microsoft.Data.SqlClient;
using SyncCB3.Entity;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;

namespace SyncCB3.DataAceess
{
    
    public class DAOSYNC
	{
        string Aplicacion = "Sincronizacion CB3";
        string Servicio = "DAOSYNC.cs";
        string Metodo = string.Empty;
        private string cnCB3
		{
			get
			{
				return ConfigurationManager.ConnectionStrings["cnCB3"]?.ConnectionString
					?? throw new InvalidOperationException("Connection string 'cnCB3' is not configured.");
			}
		}

		public List<customer> GetCustomersSyncCB3(string CveCiudad)
		{
			Metodo = "GetCustomersSyncCB3";
			Aplicacion = "Syncroniza CB3";

			List<customer> salida = new List<customer>();

			try
			{
				SqlConnectionStringBuilder connectionStringBuilder = new SqlConnectionStringBuilder(this.cnCB3)
				{
					TrustServerCertificate = true
				};

				using SqlConnection conexion = new SqlConnection(connectionStringBuilder.ConnectionString);
				using SqlCommand command = new SqlCommand("sp_Customers_GetAll", conexion);

				command.CommandType = CommandType.StoredProcedure;
				command.CommandTimeout = 120;
				command.Parameters.Add("@CveCiudad", SqlDbType.NVarChar).Value = CveCiudad;

				conexion.Open();
				using SqlDataReader dr = command.ExecuteReader();

				if (dr.HasRows)
				{
					while (dr.Read())
					{
						customer elemento = new customer
						{
							cust_id = dr.GetInt32(0),
							cust_code = dr.GetInt32(1),
							cust_name = dr.GetString(2),
							cust_first_name = dr.GetString(2),
							cust_phone = dr.GetString(4),
							cust_mobile = dr.GetString(5),
							cust_address = dr.GetString(8),
							cust_fax = dr.GetString(6),
							cust_email = dr.GetString(7),
							cust_udf1 = "1",
							cust_udf2 = "3"
						};

						salida.Add(elemento);
					}
				}
			}
			catch (Exception ex)
			{
				EscribirBitacora_cb3(Aplicacion, Servicio, Metodo, ex.Message);
			}

			return salida;
		}

        public string ObtenerNombreArchivo_cb3()
        {
            string Archivo = string.Empty;
            Archivo = "SYNC_CB3_" + DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString() + DateTime.Now.Day.ToString() + ".txt";
            return Archivo;
        }
        public void EscribirBitacora_cb3(string Aplicacion, string Modulo, string Funcion, string Error)
        {
            string strLogMessage = string.Empty;
            string strLogFile = ObtenerNombreArchivo_cb3();
            StreamWriter swLog;

            strLogMessage = DateTime.Now.ToString() + "|" + Aplicacion + "|" + Modulo + "|" + Funcion + "|" + Error;//string.Format("{0}: {1}", DateTime.Now, MensajeError);

            if (!File.Exists(strLogFile))
            {
                swLog = new StreamWriter(strLogFile);
            }
            else
            {
                swLog = File.AppendText(strLogFile);
            }
            swLog.WriteLine(strLogMessage);
            swLog.Close();
            //InsertaBitacora(Aplicacion, Modulo, Funcion, Error);

        }
    }
}
