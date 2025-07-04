using System.IO;
using Zene.Graphics;
using Zene.Structs;

namespace Mandelbrot
{
    public class Shader : BaseShaderProgram
    {
        public Shader()
        {
            Create(File.ReadAllText("./shaders/vert.glsl"),
                File.ReadAllText("./shaders/frag.glsl"), 0,
                "matrix", "uScale", "uOffset", "uMaxIter", "sh", "uPower", "uC", "uJulia");

            SetUniform(Uniforms[0], Matrix4.Identity);
            SetUniform(Uniforms[4], 7);
            SetUniform(Uniforms[5], 2);
        }
        
        private Vector2 _scale;
        public Vector2 Scale
        {
            get => _scale;
            set
            {
                _scale = value;
                SetUniform(Uniforms[1], value);
            }
        }
        private Vector2 _offset;
        public Vector2 Offset
        {
            get => _offset;
            set
            {
                _offset = value;
                SetUniform(Uniforms[2], value);
            }
        }
        private Vector2 _c;
        public Vector2 C
        {
            get => _c;
            set
            {
                _c = value;
                SetUniform(Uniforms[6], value);
            }
        }
        private int _maxIter;
        public int MaxIter
        {
            get => _maxIter;
            set
            {
                _maxIter = value;
                SetUniform(Uniforms[3], value);
            }
        }
        private int _sh;
        public int SH
        {
            get => _sh;
            set
            {
                _sh = value;
                SetUniform(Uniforms[4], value);
            }
        }
        private int _power;
        public int Power
        {
            get => _power;
            set
            {
                _power = value;
                SetUniform(Uniforms[5], value);
            }
        }
        private bool _julia;
        public bool Julia
        {
            get => _julia;
            set
            {
                _julia = value;
                SetUniform(Uniforms[7], value);
            }
        }
    }
}