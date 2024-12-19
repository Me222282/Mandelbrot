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
                File.ReadAllText("./shaders/frag.glsl"),
                "matrix", "uScale", "uOffset", "uMaxIter", "sh");
            
            _m2m3 = new MultiplyMatrix4(null, null);
            _m1Mm2m3 = new MultiplyMatrix4(null, _m2m3);

            SetUniform(Uniforms[0], Matrix.Identity);
            SetUniform(Uniforms[4], 7);
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
        
        public override IMatrix Matrix1
        {
            get => _m1Mm2m3.Left;
            set => _m1Mm2m3.Left = value;
        }
        public override IMatrix Matrix2
        {
            get => _m2m3.Left;
            set => _m2m3.Left = value;
        }
        public override IMatrix Matrix3
        {
            get => _m2m3.Right;
            set => _m2m3.Right = value;
        }

        private readonly MultiplyMatrix4 _m1Mm2m3;
        private readonly MultiplyMatrix4 _m2m3;
        public override void PrepareDraw()
        {
            SetUniform(Uniforms[0], _m1Mm2m3);
        }
    }
}