﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using PressPlay.FFWD.Animation;
using PressPlay.FFWD.Interfaces;
using Microsoft.Xna.Framework.Content;

namespace PressPlay.FFWD.Components
{
    public class SkinnedMeshRenderer : Component, IRenderable, Interfaces.IUpdateable
    {
        #region Content properties
        [ContentSerializer(Optional=true)]
        public string texture;
        [ContentSerializer(Optional = true)]
        public string shader;
        [ContentSerializer(Optional = true)]
        public string asset;
        [ContentSerializer(Optional = true)]
        public string mesh;
        #endregion

        [ContentSerializerIgnore]
        public Model model;
        [ContentSerializerIgnore]
        public Texture2D tex;

        private Matrix[] boneTransforms;
        private AnimationPlayer animationPlayer;
        private int meshIndex = 0;

        public override void Awake()
        {
            base.Awake();
            ContentHelper.LoadModel(asset);
            ContentHelper.LoadTexture(texture);
        }

        public override void Start()
        {
            base.Start();
            model = ContentHelper.GetModel(asset);
            tex = ContentHelper.GetTexture(texture);

            if (model == null)
            {
                return;
            }
            for (int i = 0; i < model.Meshes.Count; i++)
            {
                if (model.Meshes[i].Name == mesh)
                {
                    meshIndex = i;
                    break;
                }
            }

            boneTransforms = new Matrix[model.Bones.Count];
            // Look up our custom skinning information.
            SkinningData skinningData = model.Tag as SkinningData;
            if (skinningData != null)
            {
                // Create an animation player, and start decoding an animation clip.
                animationPlayer = new AnimationPlayer(skinningData);
                AnimationClip clip = skinningData.AnimationClips["Take 001"];
                animationPlayer.StartClip(clip);
            }
        }

        #region IUpdateable Members

        public void Update()
        {
            if (animationPlayer != null)
            {
                animationPlayer.Update(Time.deltaTime, true, Matrix.Identity);
            }
        }

        #endregion

        #region IRenderable Members
        public void Draw(SpriteBatch batch)
        {
            if (model == null)
            {
                return;
            }
            
            Matrix world = transform.world;
            
            // Do we have negative scale - if so, switch culling
            RasterizerState oldRaster = batch.GraphicsDevice.RasterizerState;
            BlendState oldBlend = batch.GraphicsDevice.BlendState;
            SamplerState oldSample = batch.GraphicsDevice.SamplerStates[0];
            if (transform.lossyScale.X < 0 || transform.lossyScale.Y < 0 || transform.lossyScale.Z < 0)
            {
                batch.GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = oldRaster.FillMode, CullMode = CullMode.CullClockwiseFace };
            }
            if (shader == "iPhone/Particles/Additive Culled")
            {
                batch.GraphicsDevice.BlendState = BlendState.Additive;
                batch.GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
            }

            // Draw the model.
            ModelMesh mesh = model.Meshes[meshIndex];
            for (int e = 0; e < mesh.Effects.Count; e++)
            {
                Matrix[] bones = new Matrix[0];
                if (animationPlayer != null)
                {
                    bones = animationPlayer.GetSkinTransforms();
                    model.CopyAbsoluteBoneTransformsTo(boneTransforms);
                }

                SkinnedEffect sEffect = mesh.Effects[e] as SkinnedEffect;
                sEffect.SetBoneTransforms(bones);
                sEffect.World = world;
                sEffect.View = Camera.main.View();
                sEffect.Projection = Camera.main.projectionMatrix;
                sEffect.EnableDefaultLighting();
                sEffect.SpecularColor = new Vector3(0.25f);
                sEffect.SpecularPower = 16;
                sEffect.Texture = tex;
                mesh.Draw();
            }

            if (transform.lossyScale.X < 0 || transform.lossyScale.Y < 0 || transform.lossyScale.Z < 0)
            {
                batch.GraphicsDevice.RasterizerState = oldRaster;
            }
            if (shader == "iPhone/Particles/Additive Culled")
            {
                batch.GraphicsDevice.BlendState = oldBlend;
                batch.GraphicsDevice.SamplerStates[0] = oldSample;
            }
        }
        #endregion
    }
}