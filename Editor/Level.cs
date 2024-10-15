using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Editor;
using System.IO;
using System.Windows.Forms;
using System.Threading;

namespace Editor
{
    internal class Level : ISerializable
    {
        public Camera GetCamera() { return m_camera; }

        private List<Models> m_models = new();
        private Camera m_camera = new(new Vector3(0, 0, -20), 16 / 9);
        private bool propertyGridDirty = false;
        private Models selectedModel = null;

        public Level() { }

        public void LoadContent(ContentManager _content)
        {
            Models teapot = new(_content, "Teapot", "Metal", "MyShader", Vector3.Zero, 15.0f);
            AddModel(teapot);
        }

        public void AddModel(Models _model)
        {
            m_models.Add(_model);
        }

        public List<Models> GetSelectedModels()
        {
            List<Models> models = new List<Models>();
            foreach (var model in m_models)
            {
                if (model.Selected) models.Add(model);
            }
            return models;
        }

        public void Render()
        {
            foreach (Models m in m_models)
            {
                m.Render(m_camera.View, m_camera.Projection);
            }
        }

        private void HandleTranslate()
        {
            InputController ic = InputController.Instance;
            Vector3 translate = Vector3.Zero;
            if (ic.IsKeyDown(Keys.Left)) translate.X += -10;
            if (ic.IsKeyDown(Keys.Right)) translate.X += 10;
            if (ic.IsKeyDown(Keys.Menu))
            {
                if (ic.IsKeyDown(Keys.Up)) translate.Z += 1;
                if (ic.IsKeyDown(Keys.Down)) translate.Z += -1;
            }
            else
            {
                if (ic.IsKeyDown(Keys.Up)) translate.Y += 10;
                if (ic.IsKeyDown(Keys.Down)) translate.Y += -10;
            }
            if (ic.IsButtonDown(MouseButtons.Middle))
            {
                Vector2 dir = ic.MousePosition - ic.LastPosition;
                translate.X = dir.X;
                translate.Y = dir.Y;
            }
            if (ic.GetWheel() != 0)
            {
                translate.Z = ic.GetWheel() * 2;
            }
            if (translate != Vector3.Zero)
            {
                bool modelTranslated = false;
                foreach (Models model in m_models)
                {
                    if (model.Selected)
                    {
                        modelTranslated = true;
                        model.Translate(translate / 1000, m_camera);
                    }
                }
                if (!modelTranslated)
                {
                    m_camera.Translate(translate * 0.001f);
                }
            }
        }

        private void HandleRotate(float _delta)
        {
            InputController ic = InputController.Instance;
            if ((ic.IsButtonDown(MouseButtons.Right)) && (!ic.IsKeyDown(Keys.Menu)))
            {
                Vector2 dir = ic.MousePosition - ic.LastPosition;
                if (dir != Vector2.Zero)
                {
                    Vector3 movement = new Vector3(dir.Y, dir.X, 0) * _delta;
                    bool modelRotated = false;
                    foreach (Models model in m_models)
                    {
                        if (!model.Selected)
                        {
                            modelRotated = true;
                            model.Rotation += movement;
                        }
                    }
                    if (!modelRotated)
                    {
                        m_camera.Rotate(movement);
                    }
                }
            }
        }

        private void HandleScale(float _delta)
        {
            InputController ic = InputController.Instance;
            if ((ic.IsButtonDown(MouseButtons.Right)) && (ic.IsKeyDown(Keys.Menu)))
            {
                float l = ic.MousePosition.X - ic.LastPosition.X;
                if (l != 0)
                {
                    l *= _delta;
                    foreach (Models model in m_models)
                    {
                        if (model.Selected)
                        {
                            model.Scale += l;
                        }
                    }
                }
            }
        }

        private void HandlePick()
        {
            InputController ic = InputController.Instance;
            if (ic.IsButtonDown(MouseButtons.Left))
            {
                Ray r = ic.GetPickRay(m_camera);
                foreach (Models model in m_models)
                {
                    bool wasSelected = model.Selected;
                    model.Selected = false;
                    foreach (ModelMesh mesh in model.Mesh.Meshes)
                    {
                        BoundingSphere s = mesh.BoundingSphere;
                        s = s.Transform(model.GetTransform());
                        float? f = r.Intersects(s);
                        if (f.HasValue)
                        {
                            model.Selected = true;
                        }
                    }
                    if (model.Selected && !wasSelected)
                    {
                        selectedModel = model;
                        propertyGridDirty = true;
                    }
                }
            }
        }

        public void Update(float _delta)
        {
            HandleTranslate();
            HandleRotate(_delta);
            HandleScale(_delta);
            HandlePick();
        }

        public void Serialize(BinaryWriter _stream)
        {
            _stream.Write(m_models.Count);
            foreach (var model in m_models)
            {
                model.Serialize(_stream);
            }
            m_camera.Serialize(_stream);
        }

        public void Deserialize(BinaryReader _stream, ContentManager _content)
        {
            int modelCount = _stream.ReadInt32();
            for (int count = 0; count < modelCount; count++)
            {
                Models m = new();
                m.Deserialize(_stream, _content);
                m_models.Add(m);
            }
            m_camera.Deserialize(_stream, _content);
        }

        public override string ToString()
        {
            string s = string.Empty;
            foreach (Models m in m_models)
            {
                if (m.Selected)
                {
                    s += "\nModel: Pos: " + m.Position.ToString() + "  Rot: " + m.Rotation.ToString();
                }
            }
            return m_camera.ToString() + s;
        }
    }
}