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

namespace Editor
{
    internal class Level : ISerializable
    {
        public Camera GetCamera() { return m_camera; }

        //Members
        private List<Models> m_models = new();
        private Camera m_camera = new(new Vector3(0, 0, 300), 16 / 9);
        private Models sunModel;
        private List<Models> worldModels = new();
        private const int MaxWorlds = 5;
        private List<Models> moonModels = new();


        public Level() { }

        public void LoadContent(ContentManager _content)
        {
            Models teapot = new(_content, "Teapot", "Metal", "MyShader", Vector3.Zero, 1.0f);
            AddModel(teapot);
        }


        public void AddSun(ContentManager _content)
        {
            if (sunModel == null)
            {
                sunModel = new Models(_content, "Sun", "SunDiffuse", "MyShader", Vector3.Zero, 2.0f);
                AddModel(sunModel);
            }
        }

        public void AddWorld(ContentManager _content)
        {
            if (worldModels.Count < MaxWorlds)
            {
                Vector3 randomPosition = new Vector3(
                    RandomHelper.GetRandomFloat(-150, 150),
                    RandomHelper.GetRandomFloat(-90, 90),
                    0);

                Models worldModel = new Models(_content, "World", "WorldDiffuse", "MyShader", randomPosition, 0.75f);
                worldModels.Add(worldModel);
                AddModel(worldModel);
            }
        }

        public void AddMoon(ContentManager _content)
        {
            foreach (var world in worldModels)
            {
                Vector3 moonPosition = world.Position + new Vector3(20, 0, 0);  // Moon offset from world

                Models moonModel = new Models(_content, "Moon", "MoonDiffuse", "MyShader", moonPosition,
                    RandomHelper.GetRandomFloat(0.2f, 0.4f));

                moonModels.Add(moonModel);
                AddModel(moonModel);
            }
        }

        public void AddModel(Models _model)
        {
            m_models.Add(_model);
        }
        public void Render()
        {
            foreach (Models m in m_models)
            {
                m.Render(m_camera.View, m_camera.Projection);
            }
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
    }
}
