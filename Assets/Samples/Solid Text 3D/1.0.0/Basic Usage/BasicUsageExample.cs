using UnityEngine;
using MasaChuang.SolidText3D;

namespace MasaChuang.SolidText3D.Samples
{
    /// <summary>
    /// スクリプトから SolidText3DComponent のテキストを動的に変更するサンプル。
    /// SolidText3DComponent がアタッチされた GameObject にこのスクリプトをアタッチしてください。
    /// </summary>
    public class BasicUsageExample : MonoBehaviour
    {
        private SolidText3DComponent _text3D;
        private int _score;

        private void Start()
        {
            _text3D = GetComponent<SolidText3DComponent>();
            if (_text3D == null)
            {
                Debug.LogError("[BasicUsageExample] SolidText3DComponent が見つかりません。");
                return;
            }

            UpdateScoreText();
        }

        private void Update()
        {
            // スペースキーでスコアを増加させてテキストを動的に更新
            if (Input.GetKeyDown(KeyCode.Space))
            {
                _score += 100;
                UpdateScoreText();
            }
        }

        private void UpdateScoreText()
        {
            if (_text3D != null)
                _text3D.Text = $"Score: {_score}";
        }
    }
}
