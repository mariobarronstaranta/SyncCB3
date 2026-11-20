using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
// Referencias agregadas para proceso de validacion
// del microservicio
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using SyncCB3.Entity;
using SyncCB3.Process;
using System.Text.RegularExpressions;
using static System.Net.WebRequestMethods;

namespace SyncCB3.UITest
{
    public partial class PruebaAPI : Form
    {

        // HttpClient como campo readonly para reutilización
        private readonly HttpClient _httpClient;
        StringBuilder sb = new StringBuilder();

        string urlCustomers = ConfigReader.UrlPutCustomers;
        string urlOrders = ConfigReader.UrlPutOrders;
        string urlGetOrders = ConfigReader.UrlGetOrders;


        public PruebaAPI()
        {
            InitializeComponent();
            _httpClient = CreateHttpClient();

            txtURL.Text = urlCustomers;
            txtUrlPedidos.Text = urlOrders;
            txtUrlGetOrders.Text = urlGetOrders;
        }

        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(30)
            };

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            return client;
        }
        List<customer> LeerClientes(string CveCiudad)
        {
            List<customer> salida = new List<customer>();
            Process.SyncCustomer Proceso = new SyncCustomer();
            salida = Proceso.GetCustomersSyncCB3("MTY");
            return salida;
        }

        private async void btnAccion_Click(object sender, EventArgs e)
        {
            List<customer> clientesCB3 = new List<customer>();
            clientesCB3 = LeerClientes("MTY");
            foreach (customer cliente in clientesCB3)
            {
                await ACCION(cliente);
            }
        }

        public string LimpiaCaracteres(string cadena)
        {
            if (string.IsNullOrEmpty(cadena))
                return cadena;

            // Expresión regular que coincide con caracteres no válidos en URI
            string patron = @"[^\w\-\.\~\/\?\#\[\]\@\!\$\&\'\(\)\*\+\,;\=:]";
            return Regex.Replace(cadena, patron, " ");
        }

        private async Task ACCION(customer cliente)
        {
            // Validación de la URL
            if (!Uri.TryCreate(txtURL.Text, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                sb.Append("Por favor ingrese una URL válida (HTTP/HTTPS) Error de validación");
                sb.Append(Environment.NewLine);

                /* MessageBox.Show("Por favor ingrese una URL válida (HTTP/HTTPS)",
                               "Error de validación",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Error);
                 return;*/
            }

            try
            {


                // Crear el objeto JSON a enviar
                var request_Data = new
                {
                    customers = new[]
                    {
                    new
                    {
                        customer = new
                        {
                            cust_code       = cliente.cust_code,
                            cust_name       = LimpiaCaracteres(cliente.cust_name),
                            cust_first_name = LimpiaCaracteres(cliente.cust_first_name),
                            cust_phone      = LimpiaCaracteres(cliente.cust_phone),
                            cust_mobile     = LimpiaCaracteres(cliente.cust_mobile),
                            cust_address    = LimpiaCaracteres(cliente.cust_address),
                            cust_fax        = LimpiaCaracteres(cliente.cust_fax),
                            cust_email      = LimpiaCaracteres(cliente.cust_email)//,
                            //cust_udf1       = "1",
                            //cust_udf2       = "1"
                        }
                    }
                }
                };

                // Serialización a JSON
                var jsonContent = JsonConvert.SerializeObject(request_Data);
                using (var content = new StringContent(jsonContent, Encoding.UTF8, "application/json"))
                {

                    sb.Append(jsonContent.ToString());
                    sb.Append(Environment.NewLine);

                    txtMensajes.Text = sb.ToString();

                    // Mostrar indicador de progreso
                    Cursor.Current = Cursors.WaitCursor;

                    // Realizar la llamada POST
                    using (var response = await _httpClient.PostAsync(uri.AbsoluteUri, content))
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        if (response.IsSuccessStatusCode)
                        {
                            // Intentar deserializar la respuesta
                            try
                            {
                                var apiResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);
                                sb.Append(apiResponse?.Message);
                                sb.Append(Environment.NewLine);
                                sb.Append(responseContent);
                                sb.Append(Environment.NewLine);

                                txtMensajes.Text = sb.ToString();
                                /*
                                MessageBox.Show($"Operación exitosa: {apiResponse?.Message ?? responseContent}",
                                              "Éxito",
                                              MessageBoxButtons.OK,
                                              MessageBoxIcon.Information);*/
                            }

                            catch
                            {
                                sb.Append(responseContent);
                                sb.Append(Environment.NewLine);

                                txtMensajes.Text = sb.ToString();
                                /*
                                MessageBox.Show($"Operación exitosa: {responseContent}",
                                                "Éxito",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Information);
                                */
                            }
                        }
                        else
                        {
                            sb.Append("Error en la API" + response.StatusCode.ToString());
                            sb.Append(Environment.NewLine);
                            sb.Append(responseContent);
                            sb.Append(Environment.NewLine);

                            txtMensajes.Text = sb.ToString();
                            /*
                            MessageBox.Show($"Error en la API: {response.StatusCode}\n{responseContent}",
                                          "Error",
                                          MessageBoxButtons.OK,
                                          MessageBoxIcon.Error);
                            */
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                sb.Append("La solicitud ha excedido el tiempo de espera");
                sb.Append(Environment.NewLine);
                txtMensajes.Text = sb.ToString();

                /* MessageBox.Show("La solicitud ha excedido el tiempo de espera",
                               "Timeout",
                               MessageBoxButtons.OK,
                               MessageBoxIcon.Warning); */
            }
            catch (HttpRequestException httpEx)
            {
                sb.Append("Error de conexión: " + httpEx.Message + " Error de red");
                sb.Append(Environment.NewLine);
                txtMensajes.Text = sb.ToString();

                /*
                MessageBox.Show($"Error de conexión: {httpEx.Message}",
                              "Error de red",
                              MessageBoxButtons.OK,
                              MessageBoxIcon.Error);
                */
            }
            catch (Exception ex)
            {
                sb.Append("Error de conexión: " + ex.Message + " Error");
                sb.Append(Environment.NewLine);
                txtMensajes.Text = sb.ToString();
                /*
                                MessageBox.Show($"Error inesperado: {ex.Message}",
                                                "Error",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error);
                */
            }
            finally
            {
                Cursor.Current = Cursors.Default;
            }
        }
    }
}
