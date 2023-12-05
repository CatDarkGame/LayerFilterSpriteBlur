using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.RendererFeature
{
    public class LayerFilterRendererPass_Drawpass : ScriptableRenderPass
    {
        private const string k_ProfilingSamplerName = "LayerFilterDrawpass";

        private ProfilingSampler m_ProfilingSampler;

        private LayerMask _layerMask;
        private ShaderTagId _shaderTagId;

        public LayerFilterRendererPass_Drawpass(RenderPassEvent passEvent, LayerMask layerMask, ShaderTagId shaderTagId)
        {
            renderPassEvent = passEvent;
            _layerMask = layerMask;
            _shaderTagId = shaderTagId;

            m_ProfilingSampler = new ProfilingSampler(k_ProfilingSamplerName);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(ScriptableRenderPassInput.None);     // 필요한 렌더버퍼 명시 함수, Copypass에서 원본 패스로 전환하기 위해 사용
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new UnityEngine.Rendering.ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                DrawingSettings drawSettings = CreateDrawingSettings(_shaderTagId, ref renderingData, SortingCriteria.CommonTransparent);
                FilteringSettings filterSetting = new FilteringSettings(RenderQueueRange.transparent, _layerMask);
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSetting);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }
}