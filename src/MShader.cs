using System.IO;
using Zene.Graphics;
using Zene.Structs;

namespace Mandelbrot
{
    public interface ISetShader : IDrawingShader
    {
        public Vector2 Scale { get; set; }
        public Vector2 Offset { get; set; }
        public int MaxIter { get; set; }
        public int SH { get; set; }
    }
    
    public class MShader : BaseShaderProgram, ISetShader
    {
        public MShader()
        {
            Create(File.ReadAllText("./shaders/vert.glsl"),
                File.ReadAllText("./shaders/mfrag.glsl"), 0,
                "matrix", "uScale", "uOffset", "uMaxIter", "sh");

            SetUniform(Uniforms[0], Matrix4.Identity);
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
    }
}