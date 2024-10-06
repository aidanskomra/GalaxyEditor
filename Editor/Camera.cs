﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Editor;
using System.IO;
using Microsoft.Xna.Framework.Content;

namespace Editor
{
    internal class Camera : ISerializable
    {
        public Vector3 Position { get; set; } = new Vector3(0, 0, 0);
        public Matrix View {  get; set; } = Matrix.Identity;
        public Matrix Projection { get; set; } = Matrix.Identity;
        public float NearPlane { get; set; } = 0.01f;
        public float FarPlane { get; set; } = 1000f;
        public float AspectRatio { get; set; }

        public Camera()
        {
        }

        public Camera(Vector3 _position, float _aspectRatio)
        {
            Update(_position, _aspectRatio);
        }

        public void Update(Vector3 _position, float _aspectRatio)
        {
            Position = _position;
            AspectRatio = _aspectRatio;
            View = Matrix.CreateLookAt(Position, new Vector3(0, 0, 0), Vector3.Up);
            Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45), AspectRatio, NearPlane, FarPlane);
        }
        public void Serialize(BinaryWriter _stream)
        {
            HelpSerialize.Vec3(_stream, Position);
            _stream.Write(NearPlane);
            _stream.Write(FarPlane);
            _stream.Write(AspectRatio);
        }

        public void Deserialize(BinaryReader _stream, ContentManager _content)
        {
            Position = HelpDeserialize.Vec3(_stream);
            NearPlane = _stream.ReadSingle();
            FarPlane = _stream.ReadSingle();
            AspectRatio = _stream.ReadSingle();
            Update(Position, AspectRatio);
        }
    }
}
