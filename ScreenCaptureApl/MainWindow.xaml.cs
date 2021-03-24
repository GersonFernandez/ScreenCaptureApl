using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Threading;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Drawing;
using System.Timers;
using System.Configuration;
using System.Media;
using WMPLib;
using MODI;



namespace ScreenCaptureApl
{
    /// <summary>
    /// Lógica de interacción para MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private Timer _time = null;
        private Timer _timeSound = null;
        int _interval;
        int _intervalaudio;
        int _xini;
        int _yini;
        int _width;
        int _height;
        int _countimgfail = 0;
        int _countimgpass = 0;
        int _rutrasaveimgpassIntervalo;
        int _logPass = 0;
        String _rutasaveimg = null;
        String _rutasaveimgfail = null;
        String _srutaLog = null;
        int _rutrasaveimgfailIntervalo;
        String _rutaPlayerWav = null;
        String _rutaPlayerMp3 = null;
        String _srutaComparaimg2 = null;
        SoundPlayer _playerWav;
        WindowsMediaPlayer _playerMp3;
        static Bitmap _imgBase;
        static Bitmap _imgBase2;
        Bitmap _imagCapture;


        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                    LogException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");
            this.Hide();
            InitializeComponent();
            initializeVariable();
            _time.Start();

        }


        //inicializacion de variables
        public void initializeVariable()
        {
            //int j = 0;
            //int i = 1 / j;
           
            //_playerMp3 = new WindowsMediaPlayer();
            //_playerMp3.URL = ConfigurationManager.AppSettings["RUTAMP3"];
            String _sxini = ConfigurationManager.AppSettings["XINI"];
            String _syini = ConfigurationManager.AppSettings["YINI"];
            String _swidth = ConfigurationManager.AppSettings["WIDTH"];
            String _sheight = ConfigurationManager.AppSettings["HEIGHT"];
            String _srutaComparaimg = ConfigurationManager.AppSettings["RUTACOMPARAIMG"];
            _srutaComparaimg2 = ConfigurationManager.AppSettings["RUTACOMPARAIMG2"];
            String _srutaWAV = ConfigurationManager.AppSettings["RUTAWAV"];
            String _srutaMP3 = ConfigurationManager.AppSettings["RUTAMP3"]; 
            _srutaLog = ConfigurationManager.AppSettings["RUTALOG"]; 
             _rutasaveimg = ConfigurationManager.AppSettings["RUTASAVEIMG"];
            _rutasaveimgfail = ConfigurationManager.AppSettings["RUTASAVEIMGFAILSINEXTENSION"]; 
            int.TryParse(ConfigurationManager.AppSettings["RUTASAVEIMGPASSINTERVALO"], out _rutrasaveimgpassIntervalo);
            int.TryParse(ConfigurationManager.AppSettings["RUTASAVEIMGFAILINTERVALO"],out _rutrasaveimgfailIntervalo);
            bool _binterva = int.TryParse(ConfigurationManager.AppSettings["INTERVALOMISEC"], out _interval);
            bool _bintervAudio = int.TryParse(ConfigurationManager.AppSettings["INTERVALOMISECAUDIO"], out _intervalaudio);
            //Si es 1 grabar en el log de paso la imagen si es diferente ignorar 
            bool _blogPass = int.TryParse(ConfigurationManager.AppSettings["LOGPASS"], out _logPass);
            //Console.WriteLine(Directory.GetCurrentDirectory());

            if (_blogPass == false && _logPass != 1)
            {
                _logPass = 0;
            }

            if (!String.IsNullOrEmpty(_sxini) && !String.IsNullOrEmpty(_syini)
                && !String.IsNullOrEmpty(_swidth) && !String.IsNullOrEmpty(_sheight)
                && !String.IsNullOrEmpty(_srutaComparaimg) && _binterva && _bintervAudio
                && !String.IsNullOrEmpty(_srutaLog))
            {
                try
                {

                    if (!String.IsNullOrEmpty(_srutaMP3) || !String.IsNullOrEmpty(_srutaWAV))
                    {



                        if (!String.IsNullOrEmpty(_srutaMP3))
                        {
                            _playerMp3 = new WindowsMediaPlayer();
                            _rutaPlayerMp3 = _srutaMP3;
                        }
                        if (!String.IsNullOrEmpty(_srutaWAV))
                        {
                            _playerWav = new SoundPlayer(_srutaWAV);
                        }
                    }
                    else
                    {
                        LogExceptionTXT("Ruta del audio vacia. ", "ERROR Inicializando las variables");
                        //Environment.Exit(555);
                    }
                        _time = new Timer(_interval);
                        _xini = int.Parse(_sxini);
                        _yini = int.Parse(_syini);
                        _width = int.Parse(_swidth);
                        _height = int.Parse(_sheight);
                        _time.Elapsed += new ElapsedEventHandler(TimeElapsed);
                        _imgBase = new Bitmap(_srutaComparaimg);

                    }
                    catch (Exception e)
                    {
                        LogException(e, "ERROR Inicializando las variables");
                        Environment.Exit(555);
                  

                }
                
            }
            else
            {
                LogExceptionTXT("Datos vacios necesarios edicte el app.config", "ERROR Inicializando las variables");
                Environment.Exit(555);
            }


            try
            {
                if (!String.IsNullOrEmpty(_srutaComparaimg2))
                {
                    _imgBase2 = new Bitmap(_srutaComparaimg2);
                }
                
            }
            catch
            {
                _srutaComparaimg2 = null;
            }

        }

        //Catura la imagen de la pantalla del lugar especificado
        public Bitmap CaptureScreen()
        {

            //Establece el inicio y el tamano del rectangulo de la foto
            Rectangle _limites = new Rectangle(_xini, _yini, _width, _height);
            //Inicializa la imagen con el tamano del rectangulo
            Bitmap img = new Bitmap(_limites.Width, _limites.Height);
            //convierte el de bitmap a un grafico para hacer la captura
            Graphics graf = Graphics.FromImage(img);
            //Capturar la pantalla
            graf.CopyFromScreen(new System.Drawing.Point(_limites.X, _limites.Y), System.Drawing.Point.Empty, _limites.Size);
           
            return img;

        }

        //Devuelve una lista de true y false segun la cantidad de brillo de la imagen si no tiene nada devuelve null
        public List<bool> GetHash(Bitmap _bmpSource)
        {

            List<bool> _result = new List<bool>();
            //int tr = 0;
            //int fa = 0;
            bool _separaFalse = false;

            if (IsNotNullBitmap(_bmpSource))
            {
                //reducir el tamano a la imagen a 16 x 16
                //Bitmap _bmpResize = new Bitmap(_bmpSource, new System.Drawing.Size(16, 16));
                for (int i = 0; i < _bmpSource.Height; i++)
                {
                    for (int j = 0; j < _bmpSource.Width; j++)
                    {
                        //Reduce los colores a true / false
                        if (_bmpSource.GetPixel(j, i).R > 50 && _bmpSource.GetPixel(j, i).G > 50 && _bmpSource.GetPixel(j, i).B > 50)
                        {
                            _result.Add(true);
                            //tr++;
                            _separaFalse = true;
                        }
                        else if (_separaFalse)
                        {
                            _result.Add(false);
                            //fa++;
                            _separaFalse = false;
                        }
                        else
                        {

                        }

                        //_result.Add(_bmpResize.GetPixel(j, i).GetBrightness() < 0.5f);
                    }
                }
                //Console.WriteLine("Contador white: " + tr);
                //Console.WriteLine("Contador Total: " + fa);
                return _result;
            }
            else
            {
                return null;
            }


        }


        //valida los datos de la lista que no sea nulo y que contenga valores
        public bool HaveElement(IEnumerable<bool> data)
        {

            return data != null && data.Any();
        }

        //Devuelve el numero de elementos iguales en la lista
        public int NumberEqualElement(List<bool> _imag1, List<bool> _imag2)
        {
            int _count;
            int _elementEqual = 0;

            if (HaveElement(_imag1) && HaveElement(_imag2))
            {


                _count = MinNumber(_imag1.Count, _imag2.Count);

                for (int i = 0; i < _count; i++)
                {
                    if (_imag1[i] == _imag2[i])
                    {
                        _elementEqual++;
                    }

                }
            }

            return _elementEqual;

        }

        //Devuelve el numero menor de los dos
        public int MinNumber(int i, int j)
        {
            if (i <= j)
            {
                return i;
            }
            else
            {
                return j;
            }
        }

        //Devuelve el numero de elementos diferentes en la lista
        public int NumberDiffElement(List<bool> _imag1, List<bool> _imag2)
        {
            int _count;
            int _elementDiff = 0;

            if (HaveElement(_imag1) && HaveElement(_imag2))
            {

                _count = MinNumber(_imag1.Count, _imag2.Count);

                for (int i = 0; i < _count; i++)
                {
                    if (_imag1[i] != _imag2[i])
                    {
                        _elementDiff++;
                    }

                }

            }

            return _elementDiff;

        }

        //Devuelve el porciento de un numero pasando el numero de elementos y el total de los elementos vasado en 100
        public int Percent(int _valor, int _total)
        {
            if(_total <= 0)
            {
                return 0;
            }
            else
            {
                return ((100 * _valor) / _total);
            }
            
        }

        //Devuelve true si el Bitmap no es nulo
        public bool IsNotNullBitmap(Bitmap _bmp)
        {
            return _bmp != null;
        }


        public bool ComparaScreen(Bitmap _img1,Bitmap _img2)
        {

            //int percent = Percent(NumberEqualElement(GetHash(_imgBase), GetHash(CaptureScreen())), MinNumber(GetHash(_imgBase).Count, GetHash(CaptureScreen()).Count));
            //List<bool> _imag1 = GetHash(new Bitmap(@"C:\prueba\cajero recortada 1.png"));

            List<bool> _imagCaptList = GetHash(_img1);
            List<bool> _imagBaseList = GetHash(_img2);
            int _elemenEqual = NumberEqualElement(_imagBaseList, _imagCaptList);
            int _elemenDiff = NumberDiffElement(_imagBaseList, _imagCaptList);
            int percent = Percent(_elemenEqual, MinNumber(_imagBaseList.Count, _imagCaptList.Count));

            if (percent == 100)
            {
                return true;
            }
            else
            {
                return false;
            }

            //Bitmap gris = CaptureScreen();
            //gris =ConverGray(gris);
            //gris.Save(@"C:\prueba\gris.png");
            //Console.WriteLine("Este es el imga1 count = " + _imagBaseList.Count);
            //Console.WriteLine("Este es el imga2 count = " + _imagCaptList.Count);
            //Console.WriteLine("Este es el porciento = " + percent);
            //MessageBox.Show("Este es el porciento: " + percent + " Equal: " + _elemenEqual + " Diff: " + _elemenDiff);
        }

        //Reproduce un sonido pasado por el archivo de configuracion 
        //Reproduce el MP3 si es nulo entonces reproduce el WAV
        public void Reproduce()
        {

            try
            {
                if (_rutaPlayerMp3 != null)
                {
                    //Timer _timeWait = new Timer(10000);
                    //TimeSpan interval = new TimeSpan(0, 0, 40);

                    //_playerWav.LoadAsync();
                    _playerMp3.URL = _rutaPlayerMp3;
                    // _time.Stop();
                    //_time.Interval = _intervalaudio;
                    //_timeSound.Start();
                    //_timeWait.Start();
                    //_timeWait.Elapsed += OnTimedEvent;
                    //_time.Start();
                    //_playerWav.PlaySync();

                }
                else if (_rutaPlayerWav != null)
                {
                    //_time.Stop();
                    //_time.Interval = _intervalaudio;
                    //_time.Start();
                    _playerWav.Play();
                }
                
            }
            catch (Exception ex)          
            {
                LogException(ex,"ERROR AUDIO");
            }
            finally
            {
                _time.Interval = _intervalaudio;
            }
             

        }
        //si pasa la comprobacion
        public void PassImg(Bitmap _imagCapture)
        {
            //la guarda en una ruta especificada
            if (!String.IsNullOrEmpty(_rutasaveimg))
            {

                if (_countimgpass == _rutrasaveimgpassIntervalo)
                {
                    _countimgpass = 0;

                }
                else
                {
                    _countimgpass++;
                }

                String ruta = _rutasaveimg + _countimgpass + ".png";
                _imagCapture.Save(@ruta);

            }
            if(_logPass == 1)
            {
                LogExceptionTXT("Imagenes iguales", "imagenes iguales");
            }

            Reproduce();
        }
        //si falla la comprobacion
        public void FailImg(Bitmap _imagCapture)
        {
            if (!String.IsNullOrEmpty(_rutasaveimgfail))
            {


                if (_countimgfail == _rutrasaveimgfailIntervalo)
                {
                    _countimgfail = 0;

                }
                else
                {
                    _countimgfail++;
                }

                String ruta = _rutasaveimgfail + _countimgfail + ".png";
                _imagCapture.Save(ruta);
            }
        }

        public void ComparaMensaje()
        {
             _imagCapture = CaptureScreen();
            if (!String.IsNullOrEmpty(_srutaComparaimg2))
            {
                if (ComparaScreen(_imagCapture, _imgBase) || ComparaScreen(_imagCapture,_imgBase2))
                {
                    PassImg(_imagCapture);
                }
                else
                {
                    FailImg(_imagCapture);
                }
            }
            else
            {
                if (ComparaScreen(_imagCapture, _imgBase))
                {
                    PassImg(_imagCapture);
                }
                else
                {
                    FailImg(_imagCapture);
                }
            }
           
        } 

        public int FirstTrue(List<bool> _list)
        {
            int _index = 0;
   
            bool _valida = true;
            


            while (_valida)
            {
                if (_list[_index] == true)
                {

                    _valida = false;
                }
                _index++;
            }

            return _index;

        }

        public Bitmap ConverGray(Bitmap _imag)
        {
            ////Bitmap _imag = new Bitmap(_imagBig, new System.Drawing.Size(16, 16));
            int _w = _imag.Width;
            int _h = _imag.Height;
            System.Drawing.Color _actual;
            System.Drawing.Color _nuevo;
            Bitmap _final = new Bitmap(_w, _h);
            int _contadorWhite = 0;
            int _contador = 0;


            for (int _x = 0; _x < _w; _x++)
            {
                for (int _y = 0; _y < _h; _y++)
                {
                    _actual = _imag.GetPixel(_x, _y);
                    //Console.WriteLine("actual: " + _actual);
                    //Console.WriteLine("pixeles color: "+ _imag.GetPixel(_x,_y));
                    //Console.WriteLine("color actual: " + _actual);
                    if (_actual.R > 50 && _actual.G > 50 && _actual.B > 50)
                    {
                        //Console.WriteLine("######################################################");
                        //Console.WriteLine("actual R = {0} ,actual G = {1},actual B = {2}", _actual.R,_actual.G,_actual.B);
                        //Console.WriteLine("pixeles color: " + _imag.GetPixel(_x, _y).R);
                        //Console.WriteLine("pixeles color: " + _imag.GetPixel(_x, _y).G);
                        //Console.WriteLine("pixeles color: " + _imag.GetPixel(_x, _y).B);
                        //Console.WriteLine("######################################################");

                        _contadorWhite++;
                        _nuevo = System.Drawing.Color.FromArgb(255, 0, 0);
                        //Console.WriteLine("X = "+_x+"y = "+_y);
                    }
                    else
                    {
                        _nuevo = System.Drawing.Color.FromArgb(0, _actual.R, _actual.G, _actual.B);
                    }
                    //_nuevo = System.Drawing.Color.FromArgb(_actual.R, _actual.R, _actual.R);
                    //Console.WriteLine("Nuevo: " + _nuevo);
                    //if (_nuevo.Equals(System.Drawing.Color.White))
                    //{
                    //    _contadorWhite++;
                    //}
                    _contador++;
                    _final.SetPixel(_x, _y, _nuevo);
                }
            }

            Console.WriteLine("Contador white: " + _contadorWhite);
            Console.WriteLine("Contador Total: " + _contador);
            return _final;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
           
            //reproduce();
            _time.Start();
            //ReadTextFromImag();
        }


        private void TimeElapsed(object sender, ElapsedEventArgs e)
        {

            try
            {

                //_playerWav.Play();
                //_timeSound.Stop();
                //_time.Start();
                //reproduce();
                //_time.Stop();
                //if (_time.Enabled == true)
                //{
                    
                _time.Interval = _interval;
                //_time.Start();
                //_timeSound.Enabled = false;
                //    //_timeSound.Stop();
                //}
                //else
                //{
                //Console.WriteLine("Bien");
               
                ComparaMensaje();
                    
                //}
                //Console.WriteLine("Estamos medio durmiendo!!!!");
                //System.Threading.Thread.Sleep(20000);
                //Console.WriteLine("Estamos durmiendo!!!!");

                //ComparaScreen();
                //_playerMp3.URL = ConfigurationManager.AppSettings["RUTAMP3"];
                //_playerMp3.controls.play();
                //CaptureScreen();

            }
            catch (Exception ex)
            {
                LogException(ex, ex.StackTrace);
                System.Diagnostics.EventLog.WriteEntry("Application", "Exception Timer  : " + ex.Message);
            }

        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            _time.Stop();

            Console.WriteLine("==========================This is color base=============================");
            Bitmap ba = new Bitmap(@"C:\prueba\favor retirela peq.jpeg");
            //int x, y;
            //x = ba.Width;
            //y = ba.Height;
            ba = ConverGray(ba);
            //Bitmap _image = new Bitmap(ba, new System.Drawing.Size(x, y));
            ba.Save(@"C:\prueba\favor retirela red peq roja 2.jpeg");
            Console.WriteLine("==========================End color base=============================");

            //Bitmap ba2 = new Bitmap(@"C:\prueba\cajero 2 pantalla fin.jpeg");
            //GetHash(ba2);
            //ComparaScreen();




        }

        //LOG para la exception guardado en la ruta de la aplicacion en txt
        private void LogExceptionTXT(String _mensage, String @event)
        {
            try
            {

                string path = @_srutaLog;
                System.IO.TextWriter tw = new System.IO.StreamWriter(path, true);
                tw.WriteLine("Fecha: " + DateTime.Now.ToString() + " Event: "+@event + " Message: " + _mensage);
                tw.Close();

            }
            catch (Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("ScreenCaptureApl", "ERROR EN EL LOG MENSAJE: " + ex.Message);
            }
           

        }
        //LOG para la exception guardado en la ruta de la aplicacion
        private void LogException(Exception exception, String @event)
        {

            string path = @_srutaLog;
            System.IO.TextWriter tw;
            try
            {

                tw = new System.IO.StreamWriter(path, true);
                tw.WriteLine("Fecha: " + DateTime.Now.ToString() + " Event: " + @event + " Message: " + exception.Message);
                tw.Close();

            }
            catch(Exception ex)
            {
                System.Diagnostics.EventLog.WriteEntry("ScreenCaptureApl", "ERROR EN EL LOG MENSAJE: "+ ex.Message);
                
            }
           

        }
    }
}
