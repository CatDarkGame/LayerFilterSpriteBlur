using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.RendererFeature
{
    public class LayerFilterRendererPass_Prepass : ScriptableRenderPass
    {
        private const string k_ProfilingSamplerName = "LayerFilterPrepass";
        private const string k_RenderTextureName = "_LayerFilterPrepassBufferRT";
        private const string k_TexturePropertyName = "_LayerFilterPrepassBufferTex";

        public RenderTargetHandle prepassBufferRTH;
        private ProfilingSampler m_ProfilingSampler;

        private LayerMask _layerMask;
        private ShaderTagId _shaderTagId;

        private bool _isClearBlack = false;

        public LayerFilterRendererPass_Prepass(RenderPassEvent passEvent, LayerMask layerMask, ShaderTagId shaderTagId, bool isClearBlack)
        {
            renderPassEvent = passEvent;
            _layerMask = layerMask;
            _shaderTagId = shaderTagId;

            m_ProfilingSampler = new ProfilingSampler(k_ProfilingSamplerName);
            _isClearBlack = isClearBlack;
        }

        public void Setup(ref RenderTargetHandle source, RenderTargetIdentifier renderTargetDestination)
        {
            source.Init(k_RenderTextureName);
            prepassBufferRTH = source;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // Prepass 렌더링할 새로운 렌더버퍼 생성 (기존 colorpass에 렌더링하는게 아닌 별도 버퍼에 렌더링하기 위함)
            RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            renderTextureDescriptor.graphicsFormat = GetGraphicsFormat();
            renderTextureDescriptor.msaaSamples = 1;
            cmd.GetTemporaryRT(prepassBufferRTH.id, renderTextureDescriptor);
            cmd.SetGlobalTexture(k_TexturePropertyName, prepassBufferRTH.Identifier());

            if(_isClearBlack) ConfigureClear(ClearFlag.All, Color.black);     // Pass 렌더링 이전에 프레임 Clear 세팅 (검은색 프레임에 Sprite 렌더링)
            ConfigureInput(ScriptableRenderPassInput.Color);
            ConfigureTarget(new RenderTargetIdentifier(prepassBufferRTH.Identifier(), 0, CubemapFace.Unknown, -1));     // Pass 렌더링 대상 세팅
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(prepassBufferRTH.id);
        }


        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new UnityEngine.Rendering.ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawSetting = CreateDrawingSettings(_shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                FilteringSettings filterSetting = new FilteringSettings(RenderQueueRange.transparent, _layerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSetting, ref filterSetting);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
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