﻿using System;
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
            _fb = new TextureRenderer(width, height);
            _fb.SetColourAttachment(0, TextureFormat.Rgb);
            _tex = _fb.GetTexture(FrameAttachment.Colour0);
            
            _scale = 4d / width;
            _offset = (width / 2d, height / 2d);
            
            _tr = new TextRenderer();
            
            _shad = new Shader();
        }
        
        private Texture2D _tex;
        private TextureRenderer _fb;
        private TextRenderer _tr;
        
        private Shader _shad;
        
        private double _scale;
        private Vector2 _offset;
        private int _maxIter = 100;
        private int _iterOff = 0;
        
        private bool _change = true;
        private double _aniSpeed = 20d;
        private Vector2 _aniPos = 0d;
        private double _end;
        private bool _animating = false;
        
        private bool _histogram = true;
        private bool _julia = false;
        private bool _useC = false;
        private Vector2 _juliaC = 0d;
        private Vector2 _mandelC = 0d;
        private Vector2 _c = 0d;
        private Vector2 _cPos = 0d;
        private bool _pointHover = false;
        private bool _pointGrab = false;
        
        protected override void OnUpdate(FrameEventArgs e)
        {
            base.OnUpdate(e);
            
            Vector2 ts = _fb.Properties.Size;
            Vector2 s = (Vector2)Size;
            
            if ((_useC || _julia) && this[Keys.Space])
            {
                _cPos = MouseLocation;
                _c = ((_cPos / s) - (_offset / ts)) * (_scale * ts);
                _change = true;
            }
            
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
                _shad.Scale = _scale * ts;
                _shad.Offset = _offset / ts;
                _shad.C = _c;
                
                dc.Model = new STMatrix(2d, 0d);
                dc.Draw(Shapes.Square);
                
                if (_histogram)
                {
                    GLArray<uint> image = _tex.GetData<uint>(BaseFormat.Rgba);
                    Histogram(image);
                    _tex.SetData(image.Width, image.Height, BaseFormat.Rgba, image);
                }
                
                _change = false;
            }
            
            e.Context.WriteFramebuffer(_fb, BufferBit.Colour, TextureSampling.Nearest);
            
            if (s.X == 0 || s.Y == 0) { return; }
            
            e.Context.Projection = Matrix4.CreateOrthographic(s.X, s.Y, 0d, 1d);
            e.Context.Model = new STMatrix(15d, (-s.X / 2d + 5d, s.Y / 2d - 5d));
            // Vector2 v = new Vector2(_mp.X - _offset.X, _mp.Y + _offset.Y) * _scale;
            // _tr.DrawCentred(e.Context, $"{v}", Shapes.SampleFont, 0, 0);
            _tr.DrawLeftBound(e.Context, $"{_maxIter}\n{1d / (_scale * s.X)}\n{e.DeltaTime:N3}", Shapes.SampleFont, 0, 0, false);
            
            if (_useC || _julia)
            {
                string i = _c.Y >= 0 ? $"+ {_c.Y:N3}i" : $"- {-_c.Y:N3}i";
                _tr.DrawLeftBound(e.Context, $"\n\n\n{_c.X:N3} {i}", Shapes.SampleFont, 0, 0, false);
                e.Context.Model = Matrix4.Identity;
                _cPos = (((_c / (_scale * ts)) + (_offset / ts)) * s) - (s * 0.5);
                ColourF c = ColourF.White;
                if (_pointHover) { c = ColourF.WhiteSmoke; }
                if (_pointGrab) { c = ColourF.Beige; }
                e.Context.DrawCircle(_cPos, 7d, c);
            }
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
        private void Histogram(GLArray<uint> image)
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
            // used colour index -> colour
            m = 1f / (float)j;
            hist[0] = 0x00000000;
            Parallel.For(0, sz, i =>
            {
                uint er = image[i] & 0x00FFFFFF;
                if (n - er < (50 << 7))
                {
                    image[i] = 0;
                    return;
                }
                uint hister = hist[er];
                
                if (hister == 0)
                {
                    image[i] = 0;
                    return;
                }
                
                float t = hister * m;
                image[i] = FUNC(400f + (300f * t));
            });
        }
        // protected override void OnSizeChange(VectorIEventArgs e)
        // {
        //     base.OnSizeChange(e);
            
        //     // _tex.SetData(e.X, e.Y, BaseFormat.R, GLArray<byte>.Empty);
        //     _change = true;
        // }
        
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
            
            if (_useC || _julia)
            {
                Vector2 mp = e.Location - (Size * 0.5);
                _pointHover = mp.SquaredDistance(_cPos) < 49d;
            }
            if (!_pan && _pointHover)
            {
                _pointGrab = true;
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
            
            _pointGrab = false;
            _pan = false;
        }
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            
            _mp = e.Location * (_fb.Properties.Size / (Vector2)Size);
            
            if (_pointGrab)
            {
                Vector2 ts = _fb.Properties.Size;
                Vector2 s = (Vector2)Size;
                _cPos = e.Location;
                _c = ((e.Location / s) - (_offset / ts)) * (_scale * ts);
                _change = true;
                return;
            }
            if (_useC || _julia)
            {
                Vector2 mp = e.Location - (Size * 0.5);
                _pointHover = mp.SquaredDistance(_cPos) < 49d;
            }
            
            // Smooth zoom
            if (_smooZoo)
            {
                _aniPos = _mp;
            }
            
            if (!_pan) { return; }
            
            _offset += _mp - _panStart;
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
            
            MaxMaxIter();
            
            Vector2 pointRelOld = (_mp - _offset) * oldZoom;
            Vector2 pointRelNew = (_mp - _offset) * newZoom;
            _offset += (pointRelNew - pointRelOld) / newZoom;
            
            _change = true;
        }
        private void MaxMaxIter()
        {
            //_maxIter = (int)(2d / (newZoom * Width)) * 10;
            _maxIter = (int)(50 * Math.Pow(Math.Log10(1d / _scale), 1.25d) / 2d) * 2 + _iterOff;
            // _maxIter = 50 + (int)Math.Pow(Math.Log10(4d / (_scale * Width)), 5d);
            
            if (_maxIter <= 5)
            {
                _maxIter = 5;
            }
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
                _histogram = !_histogram;
                _change = true;
                return;
            }
            if (e[Keys.R])
            {
                _fb.Size = Size;
                _fb.ViewSize = Size;
                _change = true;
                return;
            }
            if (e[Keys.Minus])
            {
                _iterOff -= 100;
                MaxMaxIter();
                _change = true;
                return;
            }
            if (e[Keys.Equal])
            {
                _iterOff += 100;
                MaxMaxIter();
                _change = true;
                return;
            }
            if (e[Keys.J])
            {
                _julia = !_julia;
                _shad.Julia = _julia;
                _pointHover = false;
                _pointGrab = false;
                
                if (_julia)
                {
                    if (_useC) { _mandelC = _c; }
                    _c = _juliaC;
                }
                else
                {
                    _juliaC = _c;
                    if (_useC)  { _c = _mandelC; }
                    else        { _c = 0d; }
                }
                
                _change = true;
                return;
            }
            if (e[Keys.P])
            {
                _useC = !_useC;
                _pointHover = false;
                _pointGrab = false;
                _change = true;
                
                if (!_julia)
                {
                    if (_useC) { _c = _mandelC; }
                    else
                    {
                        _mandelC = _c;
                        _c = 0d;
                    }
                }
                
                return;
            }
            if (e[Keys.D2])
            {
                _shad.Power = 2;
                _change = true;
                return;
            }
            if (e[Keys.D3])
            {
                _shad.Power = 2;
                _change = true;
                return;
            }
            if (e[Keys.D4])
            {
                _shad.Power = 2;
                _change = true;
                return;
            }
        }
    }
}
