using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CatDarkGame.RendererFeature
{
    public class LayerFilterRendererPass_Copy : ScriptableRenderPass
    {
        private const string k_ProfilingSamplerName = "LayerFilter_Copypass";
        private const string k_TexturePropertyName = "_LayerFilterCopypassBufferTex";
        private static int k_DownSampleTexPropertyName = Shader.PropertyToID("_DownsampleTex");
        private static int k_BlurOffsetPropertyName = Shader.PropertyToID("_blurOffset");
        private static int k_TexturePropertyID => Shader.PropertyToID(k_TexturePropertyName);

        private RenderTargetHandle copypassBufferRTH;
        private ProfilingSampler m_ProfilingSampler;
        private Material _material;
        private Shader _shader;

        private int _blurIteration = 3;
        private float _blurOffset = 1.0f;

        public LayerFilterRendererPass_Copy(RenderPassEvent passEvent, Shader shader)
        {
            renderPassEvent = passEvent;
            _shader = shader;

            m_ProfilingSampler = new ProfilingSampler(k_ProfilingSamplerName);
        }


        public void Setup(ref RenderTargetHandle source, int blurIteration = 3, float blurOffset = 1.0f)
        {
            copypassBufferRTH = source;
            _blurIteration = blurIteration;
            _blurOffset = blurOffset;

            if(!_material && _shader)
            {
                _material = CoreUtils.CreateEngineMaterial(_shader);
            }

            ConfigureClear(ClearFlag.None, Color.white);
            ConfigureTarget(copypassBufferRTH.id);
        }

        public void Destroy()
        {
            if (_material)
            {
                CoreUtils.Destroy(_material);
                _material = null;
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!_material || copypassBufferRTH == null) return;

            CommandBuffer cmd = CommandBufferPool.Get();
            using (new UnityEngine.Rendering.ProfilingScope(cmd, m_ProfilingSampler))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                int iteration = _blurIteration;
                int stepCount = Mathf.Max(iteration * 2 - 1, 1);
                string[] shaderIDStr = new string[stepCount];
                int[] shaderID = new int[stepCount];

                RenderTargetIdentifier identifier = new RenderTargetIdentifier(copypassBufferRTH.id);
                RenderTextureDescriptor rtdTempRT = renderingData.cameraData.cameraTargetDescriptor;
                rtdTempRT.msaaSamples = 1;

                int sourceSize_Width = rtdTempRT.width;
                int sourceSize_Height = rtdTempRT.height;

                // 다운 샘플링 Blit 반복
                for (int i = 0; i < stepCount; i++)
                {
                    int downsampleIndex = SimplePingPong(i, iteration - 1);
                    rtdTempRT.width = sourceSize_Width >> downsampleIndex + 1;
                    rtdTempRT.height = sourceSize_Height >> downsampleIndex + 1;
                    shaderIDStr[i] = k_TexturePropertyName + i.ToString();
                    shaderID[i] = Shader.PropertyToID(shaderIDStr[i]);

                    cmd.SetGlobalTexture(k_DownSampleTexPropertyName, identifier);
                    _material.SetFloat(k_BlurOffsetPropertyName, _blurOffset);

                    cmd.GetTemporaryRT(shaderID[i], rtdTempRT, FilterMode.Bilinear);
                    cmd.Blit(identifier, new RenderTargetIdentifier(shaderIDStr[i]), _material, 0);
                    if (i < stepCount - 1) identifier = new RenderTargetIdentifier(shaderIDStr[i]);
                }

                // 최종 Blit
                rtdTempRT.width = sourceSize_Width;
                rtdTempRT.height = sourceSize_Height;
                identifier = new RenderTargetIdentifier(k_TexturePropertyName);
                cmd.GetTemporaryRT(k_TexturePropertyID, rtdTempRT, FilterMode.Bilinear);
                cmd.Blit(identifier, new RenderTargetIdentifier(k_TexturePropertyName), _material, 0);

                // RT 메모리 해제
                for (int i = 0; i < stepCount; i++)
                {
                    cmd.ReleaseTemporaryRT(shaderID[i]);
                }
                cmd.ReleaseTemporaryRT(k_TexturePropertyID);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private static int SimplePingPong(int t, int max)
        {
            if (t > max) return 2 * max - t;
            return t;
        }
    }
}

