﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Windows.Forms;

namespace Editor
{
    public class GameEditor : Game
    {
        internal Project Project { get; set; }
        internal Texture DefaultTexture { get; set; }
        internal Texture2D DefaultGrass { get; set; }
        internal Texture2D DefaultHeightMap { get; set; }
        internal Effect DefaultEffect { get; set; }


        private GraphicsDeviceManager m_graphics;
        private FormEditor m_parent;
        private SpriteBatch m_spriteBatch;
        private FontController m_fonts;
        RasterizerState m_rasterizerState = new RasterizerState();
        DepthStencilState m_depthStencilState = new DepthStencilState();

        public GameEditor()
        {
            m_graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            m_rasterizerState.CullMode = CullMode.None;
            m_depthStencilState.DepthBufferEnable = true;
        }

        public GameEditor(FormEditor _parent) : this()
        {
            m_parent = _parent;
            Form gameForm = Control.FromHandle(Window.Handle) as Form;
            gameForm.TopLevel = false;
            gameForm.Dock = DockStyle.Fill;
            gameForm.FormBorderStyle = FormBorderStyle.None;
            m_parent.splitContainer3.Panel2.Controls.Add(gameForm);
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            m_spriteBatch = new SpriteBatch(GraphicsDevice);
            m_fonts = new();
            m_fonts.LoadContent(Content);
            DefaultTexture = Content.Load<Texture>("DefaultTexture");
            DefaultGrass = Content.Load<Texture2D>("DefaultGrass");
            DefaultHeightMap = Content.Load<Texture2D>("DefaultHeightMap");
            DefaultEffect = Content.Load<Effect>("DefaultShader");
        }

        private void UpdateSelected()
        {
            if (Models.SelectedDirty)
            {
                var models = Project.CurrentLevel.GetSelectedModels();
                if (models.Count == 0)
                {
                    m_parent.propertyGrid.SelectedObject = null;
                    m_parent.listBoxLevel.SelectedIndex = -1;
                }
                else if (models.Count > 1)
                {
                    m_parent.propertyGrid.SelectedObjects = models.ToArray();
                }
                else
                {
                    m_parent.propertyGrid.SelectedObject = models[0];
                    for (int count = 0; count < m_parent.listBoxLevel.Items.Count; count++)
                    {
                        ListItemLevel lil = m_parent.listBoxLevel.Items[count] as ListItemLevel;
                        if (lil.Model == models[0])
                        {
                            m_parent.listBoxLevel.SetSelected(count, true);
                        }
                    }
                }
            }
            Models.SelectedDirty = false;
        }

        protected override void Update(GameTime _gameTime)
        {
            if (Project != null)
            {
                //ScriptController.Instance.Execute("BeforeUpdateMain");
                Content.RootDirectory = Project.ContentFolder + "\\bin";
                Project.Update((float)(_gameTime.ElapsedGameTime.TotalMilliseconds / 1000));
                InputController.Instance.Clear();
                UpdateSelected();
                //ScriptController.Instance.Execute("AfterUpdateMain");
            }
            base.Update(_gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (Project != null)
            {
                //ScriptController.Instance.Execute("BeforeRenderMain");
                GraphicsDevice.RasterizerState = m_rasterizerState;
                GraphicsDevice.DepthStencilState = m_depthStencilState;
                Project.Render();
                m_spriteBatch.Begin();
                m_fonts.Draw(m_spriteBatch, 20, InputController.Instance.ToString(), new Vector2(20, 20), Color.White);
                m_fonts.Draw(m_spriteBatch, 16, Project.CurrentLevel.ToString(), new Vector2(20, 80), Color.Yellow);
                m_spriteBatch.End();
                //ScriptController.Instance.Execute("AfterRenderMain");
            }

            base.Draw(gameTime);
        }

        public void AdjustAspectRatio()
        {
            if (Project == null) return;
            Camera c = Project.CurrentLevel.GetCamera();
            c.Viewport = m_graphics.GraphicsDevice.Viewport;
            c.Update(c.Position, m_graphics.GraphicsDevice.Viewport.AspectRatio);
        }
    }
}