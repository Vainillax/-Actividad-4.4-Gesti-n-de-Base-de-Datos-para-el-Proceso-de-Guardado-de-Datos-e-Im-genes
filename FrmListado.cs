/*
 * ENCABEZADO DE AUTORÍA
 * Equipo N°: 3
 * Integrantes (orden alfabético por primer apellido):
 * 1. CONTRERAS Rodríguez Janis Isabel
 * 2. MACÍAS Cruz Meredith Miranda
 * 3. TORRES Barrera José Ángel
 */
using Biblioteca.Controllers;

namespace Biblioteca
{
    // Actividad 4.3: ventana de listado relacional. Muestra los libros con el NOMBRE de su categoría
    // (no el ID) leyendo de MariaDB (JOIN) o de MongoDB ($lookup), y la portada de cada libro al seleccionarlo.
    // Todos los controles (botones, tabla, PictureBox, Label de autoría) están creados en el Diseñador
    // (FrmListado.Designer.cs); aquí solo está la lógica.
    public partial class FrmListado : Form
    {
        private readonly ListadoController _controlador = new ListadoController();

        public FrmListado()
        {
            InitializeComponent();
            Escribir("Módulo de listado listo. Pulsa \"Preparar datos de ejemplo\" la primera vez.");
        }

        // La fecha y hora del sistema debe verse en las capturas de evidencia.
        private void Escribir(string texto)
        {
            txtBitacora.AppendText($"[{DateTime.Now:dd/MM/yyyy HH:mm:ss}] {texto}{Environment.NewLine}");
        }

        // ================== PREPARAR DATOS (tablas + registros de ejemplo en los dos motores) ==================

        private void btnPreparar_Click(object? sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                if (_controlador.PrepararMariaDB())
                    Escribir("MariaDB: tablas 'generos' y 'libros' listas con datos de ejemplo.");
                else
                    Escribir("MariaDB: no se pudieron preparar los datos.");

                if (_controlador.PrepararMongoDB())
                    Escribir("MongoDB: colecciones 'generos' y 'libros' listas con datos de ejemplo.");
                else
                    Escribir("MongoDB: no se pudieron preparar los datos.");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        // ================== LISTADO ==================

        private void btnListarMariaDB_Click(object? sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                Escribir("MariaDB: consulta ejecutada:" + Environment.NewLine + ListadoController.ConsultaJoin.Replace("\n", Environment.NewLine));
                List<LibroListado>? lista = _controlador.ListarMariaDB();
                if (lista == null)
                {
                    Escribir("MariaDB: el listado falló.");
                    return;
                }
                Mostrar(lista, "MariaDB (JOIN)");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnListarMongoDB_Click(object? sender, EventArgs e)
        {
            Cursor = Cursors.WaitCursor;
            try
            {
                Escribir("MongoDB: pipeline ejecutado:" + Environment.NewLine + ListadoController.DescripcionPipeline.Replace("\n", Environment.NewLine));
                List<LibroListado>? lista = _controlador.ListarMongoDB();
                if (lista == null)
                {
                    Escribir("MongoDB: el listado falló.");
                    return;
                }
                Mostrar(lista, "MongoDB ($lookup)");
            }
            finally
            {
                Cursor = Cursors.Default;
            }
        }

        private void btnLimpiar_Click(object? sender, EventArgs e)
        {
            dgvListado.DataSource = null;
            LiberarImagen();
            lblImagen.Text = "Selecciona un libro para ver su portada.";
            lblOrigen.Text = "Origen de datos: ninguno | Registros: 0";
        }

        // Vincula la lista al DataGridView. Las columnas se definieron en el Diseñador con encabezados
        // claros ("Categoría") y sin ningún ID técnico de llave foránea.
        private void Mostrar(List<LibroListado> lista, string motor)
        {
            dgvListado.DataSource = null;
            dgvListado.DataSource = lista;
            lblOrigen.Text = $"Origen de datos: {motor} | Registros: {lista.Count}";
            Escribir($"{motor}: {lista.Count} registro(s) listado(s).");

            if (lista.Count == 0)
            {
                LiberarImagen();
                lblImagen.Text = "No hay registros. Pulsa \"Preparar datos de ejemplo\".";
            }
        }

        // ================== IMAGEN DEL REGISTRO SELECCIONADO ==================

        private void dgvListado_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvListado.CurrentRow?.DataBoundItem is LibroListado libro)
                MostrarImagen(libro);
            else
                LiberarImagen();
        }

        // 1) Se arma la ruta física a partir de la ruta RELATIVA guardada en la base de datos.
        // 2) File.Exists comprueba que el archivo siga en el disco (si no, se avisa sin lanzar error).
        // 3) Image.FromStream sobre un FileStream dentro de "using": el archivo se lee, se copia a un
        //    Bitmap en memoria y el flujo se cierra enseguida, así el archivo no queda bloqueado
        //    (se puede borrar o reemplazar mientras la aplicación sigue abierta).
        private void MostrarImagen(LibroListado libro)
        {
            LiberarImagen();

            if (string.IsNullOrWhiteSpace(libro.RutaImagen))
            {
                lblImagen.Text = $"{libro.Titulo}\nEste libro no tiene imagen asignada.";
                return;
            }

            string rutaFisica = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, libro.RutaImagen);

            if (!File.Exists(rutaFisica))
            {
                lblImagen.Text = $"{libro.Titulo}\nImagen no encontrada: {libro.RutaImagen}";
                return;
            }

            try
            {
                using (FileStream flujo = new FileStream(rutaFisica, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (Image original = Image.FromStream(flujo))
                {
                    pbxPortada.Image = new Bitmap(original);
                }
                lblImagen.Text = $"{libro.Titulo}\n{libro.RutaImagen}";
            }
            catch (ArgumentException)
            {
                // Image.FromStream lanza esta excepción cuando el archivo no es una imagen válida.
                lblImagen.Text = $"{libro.Titulo}\nEl archivo no es una imagen válida.";
            }
            catch (IOException ex)
            {
                lblImagen.Text = $"{libro.Titulo}\nNo se pudo leer la imagen: {ex.Message}";
            }
        }

        // Libera la imagen anterior de la memoria antes de cargar otra.
        private void LiberarImagen()
        {
            pbxPortada.Image?.Dispose();
            pbxPortada.Image = null;
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            LiberarImagen();
            base.OnFormClosed(e);
        }
    }
}
