using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using uTinyRipper.Classes;
using uTinyRipper.YAML;
using uTinyRipper.SerializedFiles;

using Object = uTinyRipper.Classes.Object;

namespace uTinyRipper.AssetExporters
{
	public class YAMLAssetExporter : IAssetExporter
	{
		public bool IsHandle(Object asset)
		{
			return true;
		}

		public void Export(IExportContainer container, Object asset, string path)
		{
			Export(container, asset, path, null);
		}
		
		public void Export(IExportContainer container, Object asset, string path, Action<IExportContainer, Object, string> callback)
		{
			using (Stream fileStream = FileUtils.CreateVirtualFile(path))
			{
				using (StreamWriter streamWriter = new InvariantStreamWriter(fileStream, new UTF8Encoding(false)))
				{
					YAMLWriter writer = new YAMLWriter();
					YAMLDocument doc = asset.ExportYAMLDocument(container);
					writer.AddDocument(doc);
					writer.Write(streamWriter);
				}
			}
			callback?.Invoke(container, asset, path);
		}

		public void Export(IExportContainer container, IEnumerable<Object> assets, string path)
		{
			using (Stream fileStream = FileUtils.CreateVirtualFile(path))
			{
				using (StreamWriter streamWriter = new InvariantStreamWriter(fileStream, new UTF8Encoding(false)))
				{
					YAMLWriter writer = new YAMLWriter();
					writer.WriteHead(streamWriter);
					foreach (Object asset in assets)
					{
						YAMLDocument doc = asset.ExportYAMLDocument(container);
						writer.WriteDocument(doc);
					}
					writer.WriteTail(streamWriter);
				}
			}
		}

		public void Export(IExportContainer container, IEnumerable<Object> assets, string path, Action<IExportContainer, Object, string> callback)
		{
			throw new NotSupportedException();
		}

		public IExportCollection CreateCollection(VirtualSerializedFile virtualFile, Object asset)
		{
			if (OcclusionCullingSettings.IsSceneCompatible(asset))
			{
				if (asset.File.Collection.IsScene(asset.File))
				{
					return new SceneExportCollection(this, virtualFile, asset.File);
				}
				else
				{
					return new PrefabExportCollection(this, virtualFile, asset);
				}
			}
			else
			{
				switch (asset.ClassID)
				{
					case ClassIDType.NavMeshData:
						return new EmptyExportCollection();
					case ClassIDType.AnimatorController:
						return new AnimatorControllerExportCollection(this, virtualFile, asset);

					case ClassIDType.TimeManager:
					case ClassIDType.AudioManager:
					case ClassIDType.InputManager:
					case ClassIDType.Physics2DSettings:
					case ClassIDType.GraphicsSettings:
					case ClassIDType.QualitySettings:
					case ClassIDType.PhysicsManager:
					case ClassIDType.TagManager:
					case ClassIDType.NavMeshProjectSettings:
					case ClassIDType.NetworkManager:
					case ClassIDType.ClusterInputManager:
					case ClassIDType.UnityConnectSettings:
						return new ManagerExportCollection(this, asset);
					case ClassIDType.BuildSettings:
						return new BuildSettingsExportCollection(this, virtualFile, asset);

					case ClassIDType.MonoBehaviour:
						{
							MonoBehaviour monoBehaviour = (MonoBehaviour)asset;
							if (monoBehaviour.IsScriptableObject())
							{
								return new AssetExportCollection(this, asset);
							}
							else
							{
								// such MonoBehaviours as StateMachineBehaviour in AimatorController
								return new EmptyExportCollection();
							}
						}

					default:
						return new AssetExportCollection(this, asset);
				}
			}
		}

		public AssetType ToExportType(Object asset)
		{
			ToUnknownExportType(asset.ClassID, out AssetType assetType);
			return assetType;
		}

		public bool ToUnknownExportType(ClassIDType classID, out AssetType assetType)
		{
			assetType = AssetType.Serialized;
			return true;
		}
	}
}
