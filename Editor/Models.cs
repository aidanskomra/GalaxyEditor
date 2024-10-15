using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Editor;
using System.IO;
using System.ComponentModel;

namespace Editor
{
    class Models : INotifyPropertyChanged, ISerializable
    {

        public event PropertyChangedEventHandler PropertyChanged;

        [Browsable(false)]
        public Model Mesh { get; set; }

        [Browsable(false)]
        public Texture Texture { get; set; }

        [Browsable(false)]
        public Effect Shader { get; set; }

        [Category("Appearance")]
        [Description("Diffuse texture of the model.")]
        [TypeConverter(typeof(DiffuseTextureTypeConverter))]
        public string DiffuseTextureName
        {
            get => _diffuseTextureName;
            set => SetProperty(ref _diffuseTextureName, value, nameof(DiffuseTextureName));
        }
        private string _diffuseTextureName;

        [Category("State")]
        [Description("Selection status.")]
        public bool Selected
        {
            get => _selected;
            set => SetProperty(ref _selected, value, nameof(Selected));
        }
        private bool _selected;

        [Category("Transformation")]
        [Description("Position of the model in world space.")]
        public Vector3 Position
        {
            get => _position;
            set => SetProperty(ref _position, value, nameof(Position));
        }
        private Vector3 _position;

        [Category("Transformation")]
        [Description("Rotation of the model.")]
        public Vector3 Rotation
        {
            get => _rotation;
            set => SetProperty(ref _rotation, value, nameof(Rotation));
        }
        private Vector3 _rotation;

        [Category("Transformation")]
        [Description("Scale of the model.")]
        public float Scale
        {
            get => _scale;
            set => SetProperty(ref _scale, value, nameof(Scale));
        }
        private float _scale;

        //Members
        private Vector3 m_position;
        private Vector3 m_rotation;

        public Models()
        {
        }

        public Models(ContentManager _content, string _model, string _texture, string _effect, Vector3 _position, float _scale)
        {
            Create(_content, _model, _texture, _effect, _position, _scale);
            DiffuseTextureName = _texture;
        }
            public void Create(ContentManager _content, string _model, string _texture, string _effect, Vector3 _position, float _scale)
        {
            Mesh = _content.Load<Model>(_model);
            Mesh.Tag = _model;
            Texture = _content.Load<Texture>(_texture);
            Texture.Tag = _texture;
            Shader = _content.Load<Effect>(_effect);
            Shader.Tag = _effect;
            SetShader(Shader);
            m_position = _position;
            Scale = _scale;
            DiffuseTextureName = _texture;
        }

        private void SetProperty<T>(ref T field, T value, string propertyName)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
            }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void SetShader(Effect _effect)
        {
            Shader = _effect;
            foreach (ModelMesh mesh in Mesh.Meshes)
            {
                foreach (ModelMeshPart meshPart in mesh.MeshParts)
                {
                    meshPart.Effect = Shader;
                }
            }
        }
        public void Translate(Vector3 _translate, Camera _camera)
        {
            float distance = Vector3.Distance(_camera.Target, _camera.Position);
            Vector3 forward = _camera.Target - _camera.Position;
            forward.Normalize();
            Vector3 left = Vector3.Cross(forward, Vector3.Up);
            left.Normalize();
            Vector3 up = Vector3.Cross(left, forward);
            up.Normalize();
            Position += left * _translate.X * distance;
            Position += up * _translate.Y * distance;
            Position += forward * _translate.Z * 100f;
        }
        public void Rotate(Vector3 _rotate)
        {
            Rotation += _rotate;
        }
        public Matrix GetTransform()
        {
            return Matrix.CreateScale(Scale) *
                   Matrix.CreateFromYawPitchRoll(Rotation.Y, Rotation.X, Rotation.Z) *
                   Matrix.CreateTranslation(Position);
        }

        public void Render(Matrix _view, Matrix _projection)
        {
            Shader.Parameters["World"].SetValue(GetTransform());
            Shader.Parameters["WorldViewProjection"].SetValue(GetTransform() * _view * _projection);
            Shader.Parameters["Texture"].SetValue(Texture);
            Shader.Parameters["Tint"].SetValue(Selected);

            foreach (ModelMesh mesh in Mesh.Meshes)
            {
                mesh.Draw();
            }
        }

        public void Serialize(BinaryWriter _stream)
        {
            _stream.Write(Mesh.Tag.ToString());
            _stream.Write(Texture.Tag.ToString());
            _stream.Write(Shader.Tag.ToString());
            HelpSerialize.Vec3(_stream, Position);
            HelpSerialize.Vec3(_stream, Rotation);
            _stream.Write(Scale);
        }

        public void Deserialize(BinaryReader _stream, ContentManager _content)
        {
            string mesh = _stream.ReadString();
            string texture = _stream.ReadString();
            string shader = _stream.ReadString();
            Position = HelpDeserialize.Vec3(_stream);
            Rotation = HelpDeserialize.Vec3(_stream);
            Scale = _stream.ReadSingle();
            Create(_content, mesh, texture, shader, Position, Scale);
        }
    }
}
