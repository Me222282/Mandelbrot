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
            // Framebuffer fb = new Framebuffer();
            // fb[0] = _tex;
            TextureRenderer fb = new TextureRenderer(width, height);
            fb.SetColourAttachment(0, TextureFormat.Rgb);
            _fb = fb;
            
            _scale = 4d / width;
            _offset = (width / 2d, height / 2d);
            
            _tr = new TextRenderer();
            
            _shad = new Shader();
        }
        
        private Texture2D _tex;
        private IFramebuffer _fb;
        private TextRenderer _tr;
        
        private Shader _shad;
        
        private double _scale;
        private Vector2 _offset;
        private int _maxIter = 1500;
        
        private bool _change = true;
        private double _aniSpeed = 20d;
        private Vector2 _aniPos = 0d;
        private double _end;
        private bool _animating = false;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            Vector2 s = (Vector2)Size;
            
            if (_animating)
            {
                bool enlarge = _scale > _end;
                double m = enlarge ? 1d : -1d;
                Vector2 mp = _mp;
                _mp = _aniPos;
                OnScroll(new ScrollEventArgs(0d, m * e.DeltaTime * _aniSpeed));
                _mp = mp;
                if (enlarge ? _scale <= _end : _scale >= _end)
                {
                    _animating = false;
                }
                
                // double k = _animation / _aniTime;
                // if (k > 1d)
                // {
                //     k = 1d;
                //     _animating = false;
                // }
                // // double v = _start.Lerp(_end, k * k * k);
                // double v = 
                // Vector2 pointRelOld = ((s / 2d) - _offset) * _scale;
                // Vector2 pointRelNew = ((s / 2d) - _offset) * v;
                // _offset += (pointRelNew - pointRelOld) / v;
                // _scale = v;
                
                // _maxIter = 50 * (int)Math.Pow(Math.Log10(1d / _scale), 1.25d);
                
                // _animation += e.DeltaTime;
            }
            
            // if (_change)
            // {
                // GLArray<Colour3> ar = new GLArray<Colour3>(_tex.Width, _tex.Height);
                // Generate(ar);
                // _tex.EditData(0, 0, ar.Width, ar.Height, BaseFormat.Rgb, ar);
                
                DrawContext dc = new DrawContext(_fb, _shad);
                
                _shad.MaxIter = _maxIter;
                _shad.Scale = _scale * s;
                _shad.Offset = _offset / s;
                
                dc.Model = new STMatrix(2d, 0d);
                dc.Draw(Shapes.Square);
                e.Context.Copy(dc);
                
            //     _change = false;
            // }
            
            // _fb.CopyFrameBuffer(e.Context.Framebuffer, BufferBit.Colour, TextureSampling.Nearest);
            
            e.Context.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d);
            e.Context.Model = new STMatrix(15d, (-s.X / 2d + 5d, s.Y / 2d - 5d));
            // Vector2 v = new Vector2(_mp.X - _offset.X, _mp.Y + _offset.Y) * _scale;
            // _tr.DrawCentred(e.Context, $"{v}", Shapes.SampleFont, 0, 0);
            _tr.DrawLeftBound(e.Context, $"{_maxIter}\n{1d / (_scale * s.X)}\n{e.DeltaTime:N3}", Shapes.SampleFont, 0, 0, false);
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
            double q = num / (double)_maxIter;
            q = Math.Pow(q, 0.2);
            return (Colour3)ColourF3.FromWavelength(400f + (300f * (float)q));
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
            
            // if (_animating) { return; }
            
            double oldZoom = _scale;
            double newZoom;
            
            // zoomIn
            if (e.DeltaY > 0d)
            {
                newZoom = oldZoom - (e.DeltaY * 0.03 * oldZoom);
            }
            else
            {
                newZoom = oldZoom / (1d + e.DeltaY * 0.03);
            }

            if (newZoom < 0) { return; }

            _scale = newZoom;
            
            //_maxIter = (int)(2d / (newZoom * Width)) * 10;
            // _maxIter = 50 * (int)Math.Pow(Math.Log10(1d / _scale), 1.25d);
            // _maxIter = 50 + (int)Math.Pow(Math.Log10(4d / (_scale * Width)), 5d);
            
            Vector2 pointRelOld = (_mp - _offset) * oldZoom;
            Vector2 pointRelNew = (_mp - _offset) * newZoom;
            _offset += (pointRelNew - pointRelOld) / newZoom;
            
            _change = true;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            
            if (e[Keys.Escape])
            {
                Close();
                return;
            }
            if (e[Keys.BackSpace])
            {
                Vector2 s = Size;
                _end = 4d / s.X;
                _animating = true;
                Vector2 targetOffset = (s / 2d);
                
                // targetOffset happens to be centre
                Vector2 cp = (targetOffset - _offset) * _scale;
                _aniPos = (cp / _end) + targetOffset;
                return;
            }
        }
    }
}
