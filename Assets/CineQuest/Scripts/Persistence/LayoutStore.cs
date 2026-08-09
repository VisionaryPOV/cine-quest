// Cine Quest — Save/load monitor layouts via Core.LayoutSerializer (round-trip tested).

using System;
using System.IO;
using CineQuest.Core;
using CineQuest.Video;
using UnityEngine;

namespace CineQuest.Persistence
{
    [Serializable]
    public class LayoutData
    {
        public int version = 1;
        public string name = "Default";
        public ImageParameters image;
        public PanelPoseData mainPanel;
        public ScopePoseData[] scopes;
        public string environment = "Passthrough";
        public string qualityMode = "Balanced";
        public bool falseColor;
        public bool audioMuted = true;

        public LayoutDocument ToDocument()
        {
            var doc = new LayoutDocument
            {
                version = version,
                name = name,
                environment = environment,
                qualityMode = qualityMode,
                falseColor = falseColor,
                audioMuted = audioMuted,
                image = image != null ? image.ToState() : ImageParameterState.CreateNeutral()
            };
            if (mainPanel != null)
            {
                doc.mainPanel = new PanelPose
                {
                    id = mainPanel.id,
                    px = mainPanel.position.x,
                    py = mainPanel.position.y,
                    pz = mainPanel.position.z,
                    qx = mainPanel.rotation.x,
                    qy = mainPanel.rotation.y,
                    qz = mainPanel.rotation.z,
                    qw = mainPanel.rotation.w,
                    sx = mainPanel.scale.x,
                    sy = mainPanel.scale.y,
                    sz = mainPanel.scale.z
                };
            }
            if (scopes != null)
            {
                foreach (var s in scopes)
                {
                    if (s == null) continue;
                    doc.scopes.Add(new ScopePose
                    {
                        type = s.type,
                        enabled = s.enabled,
                        opacity = s.opacity,
                        px = s.position.x,
                        py = s.position.y,
                        pz = s.position.z,
                        qx = s.rotation.x,
                        qy = s.rotation.y,
                        qz = s.rotation.z,
                        qw = s.rotation.w,
                        sx = s.scale.x,
                        sy = s.scale.y,
                        sz = s.scale.z
                    });
                }
            }
            return doc;
        }

        public static LayoutData FromDocument(LayoutDocument doc)
        {
            if (doc == null) return null;
            var data = new LayoutData
            {
                version = doc.version,
                name = doc.name,
                environment = doc.environment,
                qualityMode = doc.qualityMode,
                falseColor = doc.falseColor,
                audioMuted = doc.audioMuted,
                image = ImageParameters.FromState(doc.image ?? ImageParameterState.CreateNeutral())
            };
            if (doc.mainPanel != null)
            {
                data.mainPanel = new PanelPoseData
                {
                    id = doc.mainPanel.id,
                    position = new Vector3(doc.mainPanel.px, doc.mainPanel.py, doc.mainPanel.pz),
                    rotation = new Quaternion(doc.mainPanel.qx, doc.mainPanel.qy, doc.mainPanel.qz, doc.mainPanel.qw),
                    scale = new Vector3(doc.mainPanel.sx, doc.mainPanel.sy, doc.mainPanel.sz)
                };
            }
            if (doc.scopes != null && doc.scopes.Count > 0)
            {
                data.scopes = new ScopePoseData[doc.scopes.Count];
                for (int i = 0; i < doc.scopes.Count; i++)
                {
                    var s = doc.scopes[i];
                    data.scopes[i] = new ScopePoseData
                    {
                        type = s.type,
                        enabled = s.enabled,
                        opacity = s.opacity,
                        position = new Vector3(s.px, s.py, s.pz),
                        rotation = new Quaternion(s.qx, s.qy, s.qz, s.qw),
                        scale = new Vector3(s.sx, s.sy, s.sz)
                    };
                }
            }
            return data;
        }
    }

    [Serializable]
    public class PanelPoseData
    {
        public string id;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    [Serializable]
    public class ScopePoseData
    {
        public string type;
        public bool enabled;
        public float opacity = 0.95f;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    public sealed class LayoutStore : MonoBehaviour
    {
        const string FileName = "cinequest_layout.json";

        public string PersistentPath => Path.Combine(Application.persistentDataPath, FileName);

        public void Save(LayoutData data)
        {
            if (data == null) return;
            data.version = 1;
            // Prefer pure LayoutSerializer so disk format matches unit-tested round-trip.
            var json = LayoutSerializer.Serialize(data.ToDocument());
            File.WriteAllText(PersistentPath, json);
            Debug.Log($"[CineQuest] Layout saved → {PersistentPath}");
        }

        public bool TryLoad(out LayoutData data)
        {
            data = null;
            if (!File.Exists(PersistentPath)) return false;
            try
            {
                var json = File.ReadAllText(PersistentPath);
                var doc = LayoutSerializer.Deserialize(json);
                data = LayoutData.FromDocument(doc);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[CineQuest] Layout load failed: {ex.Message}");
                return false;
            }
        }

        public void SaveImageOnly(ImageParameters image)
        {
            LayoutData data;
            if (!TryLoad(out data) || data == null)
                data = new LayoutData();
            data.image = image?.Clone();
            Save(data);
        }
    }
}
