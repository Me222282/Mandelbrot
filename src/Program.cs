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
            
            Window w = new Program(800, 500, "Candel");
            w.Run();
            w.Dispose();
            
            Core.Terminate();
        }
        
        public Program(int width, int height, string title)
            : base(width, height, title)
        {
            TextureRenderer fb = new TextureRenderer(width, height);
            fb.SetColourAttachment(0, TextureFormat.Rgb);
            _fb = fb;
            _tex = fb.GetTexture(FrameAttachment.Colour0);
            
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
        private int _maxIter = 1000;
        
        private bool _change = true;
        private double _aniSpeed = 20d;
        private Vector2 _aniPos = 0d;
        private double _end;
        private bool _animating = false;
        
        private bool _histergram = true;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            if (_animating)
            {
                bool enhance = _scale > _end;
                double m = enhance ? 1d : -1d;
                Vector2 mp = _mp;
                _mp = _aniPos;
                double sp = _aniSpeed;
                if (this[Mods.Shift])
                {
                    sp *= 2d;
                }
                
                OnScroll(new ScrollEventArgs(0d, m * e.DeltaTime * sp));
                _mp = mp;
                if (enhance ? _scale <= _end : _scale >= _end)
                {
                    _animating = false;
                }
            }
            
            if (_change)
            {
                DrawContext dc = new DrawContext(_fb, _shad);
                
                _shad.MaxIter = _maxIter;
                Vector2 ts = _fb.Properties.Size;
                _shad.Scale = _scale * ts;
                _shad.Offset = _offset / ts;
                
                dc.Model = new STMatrix(2d, 0d);
                dc.Draw(Shapes.Square);
                
                if (_histergram)
                {
                    GLArray<uint> image = _tex.GetData<uint>(BaseFormat.Rgba);
                    Historgram(image);
                    _tex.SetData(image.Width, image.Height, BaseFormat.Rgba, image);
                }
                
                _change = false;
            }
            
            Vector2 s = (Vector2)Size;
            
            e.Context.WriteFramebuffer(_fb, BufferBit.Colour, TextureSampling.Nearest);
            
            e.Context.Projection = Matrix4.CreateOrthographic(Width, Height, 0d, 1d);
            e.Context.Model = new STMatrix(15d, (-s.X / 2d + 5d, s.Y / 2d - 5d));
            // Vector2 v = new Vector2(_mp.X - _offset.X, _mp.Y + _offset.Y) * _scale;
            // _tr.DrawCentred(e.Context, $"{v}", Shapes.SampleFont, 0, 0);
            _tr.DrawLeftBound(e.Context, $"{_maxIter}\n{1d / (_scale * s.X)}\n{e.DeltaTime:N3}", Shapes.SampleFont, 0, 0, false);
        }
        private uint FUNC(float l)
        {
            Colour3 c = Colour3.FromWavelength(l);
            uint i = c.R;
            i |= (uint)c.G << 8;
            i |= (uint)c.B << 16;
            return i;
        }
        // Source: https://stackoverflow.com/questions/53658296/mandelbrot-set-color-spectrum-suggestions/53666890#53666890
        private void Historgram(GLArray<uint> image)
        {
            float m;
            int n = (_maxIter + 50) << 7;
            uint[] hist = new uint[n + 1];
            int sz = image.Length;
            // compute histogram
            for (int i = 0; i < sz; i++)
            {
                uint er = image[i] & 0x00FFFFFF;
                if (er > hist.Length)
                {
                    continue;
                }
                hist[er]++;
            }
            // histogram -> used colour index (skip holes)
            uint j = 1;
            for (int i = 1; i <= n; i++)
            {
                if (hist[i] > 0)
                {
                    hist[i]=j;
                    j++;
                }
            }
            // used colour index -> color
            m = 1f / (float)j;
            hist[0] = 0x00000000;
            Parallel.For(0, sz, i =>
            {
                uint er = image[i] & 0x00FFFFFF;
                if (er > hist.Length)
                {
                    return;
                }
                uint hister = hist[er];
                if (j - hister < 1000)
                {
                    hister = 0;
                }
                
                if (hister == 0)
                {
                    image[i] = 0;
                    return;
                }
                
                float t = hister * m;
                image[i] = FUNC(400f + (300f * t));
            });
        }
        protected override void OnSizeChange(VectorIEventArgs e)
        {
            base.OnSizeChange(e);
            
            // _tex.SetData(e.X, e.Y, BaseFormat.R, GLArray<byte>.Empty);
            _change = true;
        }
        
        private bool _pan;
        private bool _smooZoo;
        private Vector2 _mp;
        private Vector2 _panStart;
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            
            if (e.Button == MouseButton.Middle)
            {
                _end = 0d;
                _aniPos = _mp;
                _animating = true;
                _smooZoo = true;
                return;
            }
            
            _pan = true;
            _panStart = _mp;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            
            if (e.Button == MouseButton.Middle)
            {
                _smooZoo = false;
                _animating = false;
                return;
            }
            
            _pan = false;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _mp = e.Location * (_fb.Properties.Size / (Vector2)Size);
            
            // Smooth zoom
            if (_smooZoo)
            {
                _aniPos = _mp;
            }
            
            if (!_pan) { return; }
            
            _offset += (_mp - _panStart);
            _panStart = _mp;
            _change = true;
        }
        protected override void OnScroll(ScrollEventArgs e)
        {
            base.OnScroll(e);
            
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
            _maxIter = (int)(50 * Math.Pow(Math.Log10(1d / _scale), 1.25d) / 2d) * 2;
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
                if (_animating)
                {
                    _animating = false;
                    return;
                }
                
                Vector2 s = _fb.Properties.Size;
                _end = 4d / s.X;
                _animating = true;
                
                Vector2 targetOffset = (s / 2d);
                if (e[Mods.Alt])
                {
                    // happens to be centre
                    _aniPos = targetOffset;
                    return;
                }
                
                // targetOffset happens to be centre
                Vector2 cp = (targetOffset - _offset) * _scale;
                _aniPos = (cp / _end) + targetOffset;
                return;
            }
            if (e[Keys.H])
            {
                _histergram = !_histergram;
                _change = true;
                return;
            }
        }
    }
}
