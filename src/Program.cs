using System;
using System.Threading.Tasks;
using Zene.Graphics;
using Zene.Structs;
using Zene.Windowing;

namespace Mandelbrot
{
    class Program : Window
    {
        static void Main(string[] args)
        {
            Core.Init();
            
            Window w = new Program(800, 500, "Man");
            w.Run();
            w.Dispose();
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title)
        {
            _tex = new Texture2D(TextureFormat.Rgb, TextureData.Byte);
            _tex.SetData(width, height, BaseFormat.R, GLArray<byte>.Empty);
            _fb = new Framebuffer();
            _fb[0] = _tex;
            
            _scale = 4d / width;
            _offset = (width / 2d, height / 2d);
            
            _tr = new TextRenderer();
        }
        
        private Texture2D _tex;
        private Framebuffer _fb;
        private TextRenderer _tr;
        
        private double _scale;
        private Vector2 _offset;
        private int _maxIter = 100;
        
        private bool _change = true;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            if (_change)
            {
                GLArray<Colour3> ar = new GLArray<Colour3>(_tex.Width, _tex.Height);
                Generate(ar);
                _tex.EditData(0, 0, ar.Width, ar.Height, BaseFormat.Rgb, ar);
                _change = false;
            }
            
            _fb.CopyFrameBuffer(e.Context.Framebuffer, BufferBit.Colour, TextureSampling.Nearest);
            
            // e.Context.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d);
            // e.Context.Model = Matrix4.CreateScale(15d);
            // Vector2 v = new Vector2(_mp.X - _offset.X, _mp.Y + _offset.Y) * _scale;
            // _tr.DrawCentred(e.Context, $"{v}", Shapes.SampleFont, 0, 0);
        }
        protected override void OnSizeChange(VectorIEventArgs e)
        {
            base.OnSizeChange(e);
            
            _tex.SetData(e.X, e.Y, BaseFormat.R, GLArray<byte>.Empty);
            _change = true;
        }

        private void Generate(GLArray<Colour3> map)
        {
            int w = map.Width;
            
            Parallel.For(0, w, i =>
            {
                for (int j = 0; j < map.Height; j++)
                {
                    Vector2 c = ((i - _offset.X) * _scale, (j - _offset.Y) * _scale);
                    int index = Mandelbrot(c);
                    map[i + (j * w)] = GetColour(index);
                }
            });
            
            // for (int i = 0; i < map.Width; i++)
            // {
            //     for (int j = 0; j < map.Height; j++)
            //     {
            //         Vector2 c = ((i + _offset.X) * _scale, (j + _offset.Y) * _scale);
            //         int index = Mandelbrot(c);
            //         map[i, j] = _colours[index];
            //     }
            // }
        }
        private int Mandelbrot(Vector2 c)
        {
            Vector2 z = 0d;
            
            int i = 0;
            while (z.SquaredLength <= 4d && i < _maxIter)
            {
                z = SquareComplex(z) + c;
                i++;
            }
            
            return i;
        }
        private Vector2 SquareComplex(Vector2 z) => (z.X * z.X - z.Y * z.Y, 2d * z.X * z.Y);
        
        private Colour3 GetColour(int num)
        {
            return Colour3.Orange.Lerp(Colour3.Lime, num / (_maxIter + 1d));
        }
        
        private bool _pan;
        private Vector2 _mp;
        private Vector2 _panStart;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            _pan = true;
            _panStart = e.Location;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            _pan = false;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _mp = e.Location;
            
            if (!_pan) { return; }
            
            _offset += (_mp - _panStart);// / _scale;
            _panStart = _mp;
            _change = true;
        }
        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            
            double oldZoom = _scale;
            double newZoom = oldZoom - (e.DeltaY * 0.03 * oldZoom);

            if (newZoom < 0) { return; }

            _scale = newZoom;
            
            //_maxIter = (int)(2d / (newZoom * Width)) * 10;
            _maxIter = 50 * (int)Math.Pow(Math.Log10(1d / _scale), 1.25d);
            // _maxIter = 50 + (int)Math.Pow(Math.Log10(4d / (_scale * Width)), 5d);
            
            Vector2 pointRelOld = (_mp - _offset) * oldZoom;
            Vector2 pointRelNew = (_mp - _offset) * newZoom;
            _offset += (pointRelNew - pointRelOld) / newZoom;
            
            _change = true;
        }
    }
}
