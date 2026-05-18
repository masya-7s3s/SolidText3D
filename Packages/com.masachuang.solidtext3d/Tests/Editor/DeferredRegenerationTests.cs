using NUnit.Framework;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace MasaChuang.SolidText3D.Tests.Editor
{
    public class DeferredRegenerationTests
    {
        private const string DefaultFontBytesAssetPath = "Packages/com.masachuang.solidtext3d/Runtime/Resources/Fonts/NotoSansJP-Black.bytes";
        private static readonly FieldInfo DeferredStateField = typeof(SolidText3DComponent)
            .GetField("_deferredRegenerationState", BindingFlags.NonPublic | BindingFlags.Instance);

        private static DeferredRegenerationState GetDeferredState(SolidText3DComponent component)
        {
            Assert.IsNotNull(DeferredStateField, "deferred regeneration state field が必要です。");
            return (DeferredRegenerationState)DeferredStateField.GetValue(component);
        }

        private static void InvokeTryApplyDeferredResult(SolidText3DComponent component)
        {
            var method = typeof(SolidText3DComponent).GetMethod("TryApplyDeferredResult", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "TryApplyDeferredResult が必要です。");
            method.Invoke(component, null);
        }

        private static TextStateRequest CreatePerCharacterRequest(string text, long version)
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(DefaultFontBytesAssetPath);
            Assert.IsNotNull(fontAsset, "テスト用フォント bytes が必要です。");

            var generationParams = new MeshGenerationParams
            {
                Text = text,
                FontData = fontAsset.bytes,
                ExtrusionDepth = 0.25f,
                FontSize = 1f,
                HorizontalAnchor = HorizontalAnchor.Left,
                VerticalAnchor = VerticalAnchor.Lower,
                DepthAnchor = DepthAnchor.Front,
                WritingMode = WritingMode.Horizontal
            };

            return new TextStateRequest(
                version,
                new DisplayResultSignature(text, text.GetHashCode(), 12345, fontAsset.GetInstanceID(), ObjectMode.PerCharacter),
                text,
                fontAsset.bytes,
                generationParams,
                false,
                0f,
                0f,
                null,
                OutlineDisplayMode.Donut,
                ObjectMode.PerCharacter);
        }

        [Test]
        public void OutlineEnabled_False_KeepsVisibleOutlineUntilRegenerate_AndInvalidatesDeferredState()
        {
            var gameObject = new GameObject("DeferredOutlineToggleTest");
            try
            {
                var component = gameObject.AddComponent<SolidText3DComponent>();
                component.OutlineEnabled = true;

                MethodInfo ensureOutlineChild = typeof(SolidText3DComponent).GetMethod("EnsureOutlineChild", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(ensureOutlineChild);
                ensureOutlineChild.Invoke(component, null);
                Assert.IsNotNull(gameObject.transform.Find("__OutlineMesh__"));

                FieldInfo stateField = typeof(SolidText3DComponent).GetField("_deferredRegenerationState", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(stateField);
                var state = (DeferredRegenerationState)stateField.GetValue(component);
                state.LastRequestedVersion = 5;
                state.LastAppliedSignature = new DisplayResultSignature(99, 100, ObjectMode.SingleObject);
                state.PendingLatestRequest = new TextStateRequest(5, new DisplayResultSignature(5, 6, ObjectMode.SingleObject), "A", new byte[] { 1 }, default, true, 0.05f, 0.25f, null, OutlineDisplayMode.Donut, ObjectMode.SingleObject);
                state.ReadyResult = new PreparedDisplayResult
                {
                    Version = 5,
                    Signature = new DisplayResultSignature(5, 6, ObjectMode.SingleObject)
                };

                component.OutlineEnabled = false;

                Assert.IsNotNull(gameObject.transform.Find("__OutlineMesh__"), "OutlineEnabled=false の setter だけでは visible outline を即時変更しないこと");
                Assert.IsFalse(component.OutlineEnabled);
                Assert.IsNull(state.PendingLatestRequest, "状態変更時に古い pending request を破棄すること");
                Assert.IsNull(state.ReadyResult, "状態変更時に古い ready result を破棄すること");
                Assert.AreEqual(6, state.LastRequestedVersion, "状態変更時に古い in-flight result が stale 扱いになるよう version を進めること");
                Assert.AreEqual(new DisplayResultSignature(99, 100, ObjectMode.SingleObject), state.LastAppliedSignature,
                    "リジェネレート前の visible display は変わらないため same-display 基準も維持すること");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void RequestRegenerateMesh_WhenInFlight_OnlyKeepsLatestPendingRequest()
        {
            var gameObject = new GameObject("LatestOnlyDeferredTest");
            try
            {
                var component = gameObject.AddComponent<SolidText3DComponent>();
                var state = GetDeferredState(component);
                state.InFlightRequest = CreatePerCharacterRequest("10:00", 1);

                component.Text = "10:01";
                component.RequestRegenerateMesh();
                component.Text = "10:02";
                component.RequestRegenerateMesh();

                Assert.IsTrue(state.PendingLatestRequest.HasValue, "進行中 request がある間は latest request を 1 件保持すること");
                Assert.AreEqual("10:02", state.PendingLatestRequest.Value.Text,
                    "新しい request が来たら pending latest request を常に上書きすること");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void PrepareDisplayResult_PartialNumericUpdate_ReusesUnchangedPrefixGlyphs()
        {
            var previous = GlyphMeshBuilder.PrepareDisplayResult(CreatePerCharacterRequest("12:34", 1));
            var current = GlyphMeshBuilder.PrepareDisplayResult(CreatePerCharacterRequest("12:35", 2), previous);

            Assert.IsNotNull(previous.PerCharacterMeshData);
            Assert.IsNotNull(current.PerCharacterMeshData);
            Assert.AreEqual(previous.PerCharacterMeshData.Count, current.PerCharacterMeshData.Count,
                "同じ桁数の timer 更新では glyph count が維持されること");
            Assert.GreaterOrEqual(current.PerCharacterMeshData.Count, 5, "timer 表示に必要な glyph が含まれること");

            for (int index = 0; index < current.PerCharacterMeshData.Count - 1; index++)
            {
                Assert.AreSame(previous.PerCharacterMeshData[index].Vertices, current.PerCharacterMeshData[index].Vertices,
                    $"変更されていない prefix glyph {index} は prepared result 間で再利用されること");
            }

            Assert.AreNotSame(previous.PerCharacterMeshData[current.PerCharacterMeshData.Count - 1].Vertices, current.PerCharacterMeshData[current.PerCharacterMeshData.Count - 1].Vertices,
                "変更された末尾 glyph だけが再生成されること");
        }

        [Test]
        public void TryApplyDeferredResult_WhenReadyResultIsStale_DoesNotRollbackVisibleSignature()
        {
            var gameObject = new GameObject("DeferredStaleDiscardTest");
            try
            {
                var component = gameObject.AddComponent<SolidText3DComponent>();
                var state = GetDeferredState(component);
                var visibleSignature = new DisplayResultSignature("LATEST", 100, 10, 1, ObjectMode.SingleObject);
                state.LastRequestedVersion = 5;
                state.LastAppliedVersion = 4;
                state.LastAppliedSignature = visibleSignature;
                state.ReadyResult = new PreparedDisplayResult
                {
                    Version = 4,
                    Signature = new DisplayResultSignature("STALE", 90, 9, 1, ObjectMode.SingleObject)
                };

                InvokeTryApplyDeferredResult(component);

                Assert.IsNull(state.ReadyResult, "stale ready result は消費後にクリアされること");
                Assert.AreEqual(visibleSignature, state.LastAppliedSignature,
                    "stale result を捨てても visible signature は巻き戻らないこと");
                Assert.AreEqual(4, state.LastAppliedVersion, "stale result で applied version を進めないこと");
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void TextStateRequest_NormalizesNullText_AndRetainsInputs()
        {
            var outline = new OutlineSettings
            {
                Enabled = true,
                OffsetAmount = 0.2f,
                Thickness = 0.3f,
                DisplayMode = OutlineDisplayMode.Donut
            };

            var generationParams = new MeshGenerationParams
            {
                Text = "ignored",
                FontData = new byte[] { 1, 2, 3 },
                ExtrusionDepth = 0.5f,
                FontSize = 1.25f,
                HorizontalAnchor = HorizontalAnchor.Center,
                VerticalAnchor = VerticalAnchor.Middle,
                DepthAnchor = DepthAnchor.Center,
                WritingMode = WritingMode.Vertical,
                RotateAsciiInVertical = true
            };

            var signature = new DisplayResultSignature(11, 22, ObjectMode.PerCharacter);
            var request = new TextStateRequest(5, signature, null, generationParams.FontData, generationParams, true, outline.OffsetAmount, outline.Thickness, outline.Material, outline.DisplayMode, ObjectMode.PerCharacter);

            Assert.AreEqual(string.Empty, request.Text);
            Assert.AreEqual(5, request.Version);
            Assert.AreEqual(signature, request.Signature);
            Assert.AreSame(generationParams.FontData, request.FontData);
            Assert.AreEqual(ObjectMode.PerCharacter, request.ObjectMode);
            Assert.IsTrue(request.OutlineEnabled);
            Assert.AreEqual(outline.OffsetAmount, request.OutlineOffset);
            Assert.AreEqual(outline.Thickness, request.OutlineThickness);
            Assert.AreEqual(outline.Material, request.OutlineMaterial);
            Assert.AreEqual(outline.DisplayMode, request.OutlineDisplayMode);
        }

        [Test]
        public void PreparedDisplayResult_RetainsAssignedPayload()
        {
            var perCharacter = new List<GlyphMeshData>
            {
                new GlyphMeshData
                {
                    Vertices = new List<Vector3>(),
                    Triangles = new List<int>(),
                    Normals = new List<Vector3>(),
                    Offset = Vector3.one
                }
            };

            var result = new PreparedDisplayResult
            {
                Version = 7,
                Signature = new DisplayResultSignature(12, 13, ObjectMode.PerCharacter),
                PerCharacterMeshData = perCharacter,
                ClearsDisplay = true
            };

            Assert.AreEqual(7, result.Version);
            Assert.AreEqual(ObjectMode.PerCharacter, result.Signature.ObjectMode);
            Assert.AreSame(perCharacter, result.PerCharacterMeshData);
            Assert.IsTrue(result.ClearsDisplay);
        }

        [Test]
        public void DeferredRegenerationState_HasPendingWork_TracksInFlightPendingAndReady()
        {
            var state = new DeferredRegenerationState();
            Assert.IsFalse(state.HasPendingWork);

            state.PendingLatestRequest = new TextStateRequest(1, new DisplayResultSignature(1, 2, ObjectMode.SingleObject), "A", new byte[] { 1 }, default, false, 0f, 0f, null, OutlineDisplayMode.Donut, ObjectMode.SingleObject);
            Assert.IsTrue(state.HasPendingWork);

            state.PendingLatestRequest = null;
            state.InFlightRequest = new TextStateRequest(2, new DisplayResultSignature(2, 3, ObjectMode.SingleObject), "B", new byte[] { 2 }, default, false, 0f, 0f, null, OutlineDisplayMode.Donut, ObjectMode.SingleObject);
            Assert.IsTrue(state.HasPendingWork);

            state.InFlightRequest = null;
            state.ReadyResult = new PreparedDisplayResult
            {
                Version = 2,
                Signature = new DisplayResultSignature(2, 3, ObjectMode.SingleObject)
            };
            Assert.IsTrue(state.HasPendingWork);

            state.ClearReadyResult();
            Assert.IsFalse(state.HasPendingWork);
        }
    }
}