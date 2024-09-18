using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;

class TablaFAT
{
    public string NombreArchivo { get; set; }
    public string NombreArchivoDatos { get; set; }
    public bool EsReciclado { get; set; }
    public int TotalCaracteres { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime FechaModificacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    public TablaFAT(string nombreArchivo, string nombreArchivoDatos, bool esReciclado = false)
    {
        NombreArchivo = nombreArchivo;
        NombreArchivoDatos = nombreArchivoDatos;
        EsReciclado = esReciclado;
        TotalCaracteres = 0;
        FechaCreacion = DateTime.Now;
        FechaModificacion = DateTime.Now;
        FechaEliminacion = null;
    }

    public string AJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

class ArchivoDatos
{
    public string Datos { get; set; }
    public string ArchivoSiguiente { get; set; }
    public bool EsFinArchivo { get; set; }

    public ArchivoDatos(string datos, string archivoSiguiente = null, bool esFinArchivo = false)
    {
        Datos = datos;
        ArchivoSiguiente = archivoSiguiente;
        EsFinArchivo = esFinArchivo;
    }

    public string AJson()
    {
        return JsonConvert.SerializeObject(this);
    }
}

class SistemaArchivos
{
    public Dictionary<string, TablaFAT> Archivos = new Dictionary<string, TablaFAT>();

    public void CrearArchivo(string nombreArchivo, string datos)
    {
        TablaFAT tablaFAT = new TablaFAT(nombreArchivo, $"{nombreArchivo}_datos");
        Archivos[nombreArchivo] = tablaFAT;
        GuardarDatos(nombreArchivo, datos);
        GuardarTablaFAT(nombreArchivo);
    }

    public void GuardarDatos(string nombreArchivo, string datos)
    {
        List<ArchivoDatos> listaArchivosDatos = new List<ArchivoDatos>();

        for (int i = 0; i < datos.Length; i += 20)
        {
            string parte = datos.Substring(i, Math.Min(20, datos.Length - i));
            ArchivoDatos archivoDatos = new ArchivoDatos(parte, archivoSiguiente: null, esFinArchivo: (i + 20 >= datos.Length));
            listaArchivosDatos.Add(archivoDatos);
        }

        for (int i = 0; i < listaArchivosDatos.Count; i++)
        {
            string nombreArchivoDatos = $"{nombreArchivo}_datos_{i}.json";
            File.WriteAllText(nombreArchivoDatos, listaArchivosDatos[i].AJson());

            if (i < listaArchivosDatos.Count - 1)
            {
                listaArchivosDatos[i].ArchivoSiguiente = $"{nombreArchivo}_datos_{i + 1}.json";
            }
        }

        Archivos[nombreArchivo].TotalCaracteres = datos.Length;
    }

    public void GuardarTablaFAT(string nombreArchivo)
    {
        string nombreArchivoFAT = $"{nombreArchivo}_fat.json";
        File.WriteAllText(nombreArchivoFAT, Archivos[nombreArchivo].AJson());
    }

    public void ListarArchivos()
    {
        var archivos = new List<(string NombreArchivo, int TotalCaracteres, DateTime FechaCreacion, DateTime FechaModificacion)>();

        foreach (var (nombreArchivo, tablaFAT) in Archivos)
        {
            if (!tablaFAT.EsReciclado)
            {
                archivos.Add((nombreArchivo, tablaFAT.TotalCaracteres, tablaFAT.FechaCreacion, tablaFAT.FechaModificacion));
            }
        }

        for (int i = 0; i < archivos.Count; i++)
        {
            Console.WriteLine($"{i + 1}. {archivos[i].NombreArchivo} - {archivos[i].TotalCaracteres} caracteres - Creado: {archivos[i].FechaCreacion} - Modificado: {archivos[i].FechaModificacion}");
        }
    }

    public void AbrirArchivo(string nombreArchivo)
    {
        if (!Archivos.ContainsKey(nombreArchivo))
        {
            Console.WriteLine("Archivo no encontrado.");
            return;
        }

        TablaFAT tablaFAT = Archivos[nombreArchivo];
        Console.WriteLine($"Archivo: {nombreArchivo} - {tablaFAT.TotalCaracteres} caracteres - Creado: {tablaFAT.FechaCreacion} - Modificado: {tablaFAT.FechaModificacion}");

        string datos = "";
        string archivoActual = tablaFAT.NombreArchivoDatos + "_0.json";

        while (true)
        {
            if (!File.Exists(archivoActual))
            {
                Console.WriteLine($"Archivo de datos no encontrado: {archivoActual}");
                break;
            }

            string json = File.ReadAllText(archivoActual);
            ArchivoDatos archivoDatos = JsonConvert.DeserializeObject<ArchivoDatos>(json);

            if (archivoDatos == null)
            {
                Console.WriteLine($"Error al leer los datos de {archivoActual}");
                break;
            }

            datos += archivoDatos.Datos;

            if (archivoDatos.EsFinArchivo)
                break;

            archivoActual = archivoDatos.ArchivoSiguiente;
        }

        Console.WriteLine(datos);
    }

    public void ModificarArchivo(string nombreArchivo)
    {
        AbrirArchivo(nombreArchivo);
        Console.Write("Introduce los nuevos datos (presiona ESC para terminar): ");
        string nuevosDatos = Console.ReadLine();
        Console.Write("¿Quieres guardar los cambios? (s/n): ");
        string confirmar = Console.ReadLine();

        if (confirmar.ToLower() == "s")
        {
            EliminarArchivo(nombreArchivo);
            CrearArchivo(nombreArchivo, nuevosDatos);
        }
    }

    public void EliminarArchivo(string nombreArchivo)
    {
        if (!Archivos.ContainsKey(nombreArchivo)) return;

        TablaFAT tablaFAT = Archivos[nombreArchivo];
        tablaFAT.EsReciclado = true;
        tablaFAT.FechaEliminacion = DateTime.Now;
        GuardarTablaFAT(nombreArchivo);
    }

    public void RecuperarArchivo(string nombreArchivo)
    {
        if (!Archivos.ContainsKey(nombreArchivo)) return;

        TablaFAT tablaFAT = Archivos[nombreArchivo];
        tablaFAT.EsReciclado = false;
        tablaFAT.FechaEliminacion = null;
        GuardarTablaFAT(nombreArchivo);
    }

    public void Ejecutar()
    {
        while (true)
        {
            Console.WriteLine("1. Crear archivo");
            Console.WriteLine("2. Listar archivos");
            Console.WriteLine("3. Abrir archivo");
            Console.WriteLine("4. Modificar archivo");
            Console.WriteLine("5. Eliminar archivo");
            Console.WriteLine("6. Recuperar archivo");
            Console.WriteLine("7. Salir");
            Console.Write("Elige una opción: ");
            string opcion = Console.ReadLine();

            switch (opcion)
            {
                case "1":
                    Console.Write("Introduce el nombre del archivo: ");
                    string nombreArchivo = Console.ReadLine();
                    Console.Write("Introduce los datos: ");
                    string datos = Console.ReadLine();
                    CrearArchivo(nombreArchivo, datos);
                    break;
                case "2":
                    ListarArchivos();
                    break;
                case "3":
                    Console.Write("Introduce el nombre del archivo: ");
                    string nombreArchivoAbrir = Console.ReadLine();
                    AbrirArchivo(nombreArchivoAbrir);
                    break;
                case "4":
                    Console.Write("Introduce el nombre del archivo: ");
                    string nombreArchivoModificar = Console.ReadLine();
                    ModificarArchivo(nombreArchivoModificar);
                    break;
                case "5":
                    Console.Write("Introduce el nombre del archivo: ");
                    string nombreArchivoEliminar = Console.ReadLine();
                    Console.Write($"¿Estás seguro de que quieres eliminar {nombreArchivoEliminar}? (s/n): ");
                    if (Console.ReadLine().ToLower() == "s")
                    {
                        EliminarArchivo(nombreArchivoEliminar);
                    }
                    break;
                case "6":
                    Console.WriteLine("Recuperar archivo");
                    Console.Write("Introduce el nombre del archivo a recuperar: ");
                    string nombreArchivoRecuperar = Console.ReadLine();
                    RecuperarArchivo(nombreArchivoRecuperar);
                    break;
                case "7":
                    return;
                default:
                    Console.WriteLine("Opción inválida. Intenta de nuevo.");
                    break;
            }
        }
    }
}

class Programa
{
    static void Main(string[] args)
    {
        SistemaArchivos sistemaArchivos = new SistemaArchivos();
        sistemaArchivos.Ejecutar();
    }
}