using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace CatDarkGame.RendererFeature
{
    public class LayerFilterRendererPass_CopyColor : ScriptableRenderPass
    {
        private const string k_ProfilingSamplerName = "LayerFilterCopyColorpass";

        private RenderTargetIdentifier _source;
        private RenderTargetHandle _destination;
        private ProfilingSampler m_ProfilingSampler;


        public LayerFilterRendererPass_CopyColor(RenderPassEvent passEvent)
        {
            renderPassEvent = passEvent;
            m_ProfilingSampler = new ProfilingSampler(k_ProfilingSamplerName);
        }

        public void Setup(RenderTargetIdentifier source, RenderTargetHandle destination)
        {
            ConfigureInput(ScriptableRenderPassInput.Color);
            _destination = destination;
            _source = source;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            renderTextureDescriptor.graphicsFormat = GetGraphicsFormat();
            renderTextureDescriptor.msaaSamples = 1;
            cmd.GetTemporaryRT(_destination.id, renderTextureDescriptor);
            ConfigureTarget(new RenderTargetIdentifier(_destination.Identifier(), 0, CubemapFace.Unknown, -1));
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new UnityEngine.Rendering.ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                Blit(cmd, _source, _destination.Identifier());
            }
               
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(_destination.id);
        }

        public static GraphicsFormat GetGraphicsFormat()
        {
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
            {
                return GraphicsFormat.B10G11R11_UFloatPack32;
            }
            else
            {
                return QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
            }
        }
    }
}