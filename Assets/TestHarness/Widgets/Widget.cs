using System.Collections.Generic;
using UnityEngine;

public abstract class Widget : MonoBehaviour
{
	public abstract string GetResult(string key, string data);
	public virtual void Activate() { }

	[PrivateWhenPlaying] public int SizeX;
	[PrivateWhenPlaying] public int SizeZ;
	[PrivateWhenPlaying] public bool Rotate;
	public static Vector3 BaseSize = new Vector3(0.06f, 0.03f, 0.06f);
}

public class WidgetZone
{
	public WidgetZone(GameObject parent, int x, int z, int nX, int nZ, int sX, int sZ)
	{
		Parent = parent;
		X = x;
		Z = z;
		SizeX = sX;
		SizeZ = sZ;
		NumX = nX;
		NumZ = nZ;
		float num = (X + SizeX / 2f) / NumX;
		float num2 = (Z + SizeZ / 2f) / NumZ;
		float x2 = num - 0.5f;
		float z2 = num2 - 0.5f;
		LocalPosition = new Vector3(x2, 0f, z2);
		WorldRotation = parent.transform.rotation;
	}

	public Vector3 LocalPosition { get; protected set; }

	public Quaternion WorldRotation { get; protected set; }

	public static WidgetZone CreateZone(GameObject area)
	{
		float x = area.transform.lossyScale.x;
		float z = area.transform.lossyScale.z;
		int num = (int)(x / Widget.BaseSize.x);
		int num2 = (int)(z / Widget.BaseSize.z);
		return new WidgetZone(area, 0, 0, num, num2, num, num2);
	}

	public static List<WidgetZone> SubdivideZoneForWidget(WidgetZone zone, Widget widget)
	{
		List<WidgetZone> list = new List<WidgetZone>();
		int x;
		int z;
		if (widget.SizeX <= zone.SizeX && widget.SizeZ <= zone.SizeZ)
		{
			widget.Rotate = false;
			x = zone.SizeX - widget.SizeX;
			z = zone.SizeZ - widget.SizeZ;
		}
		else
		{
			if (widget.SizeX > zone.SizeZ || widget.SizeZ > zone.SizeX)
			{
				return null;
			}
			widget.Rotate = true;
			x = zone.SizeX - widget.SizeZ;
			z = zone.SizeZ - widget.SizeX;
		}
		int x2 = Random.Range(0, x + 1);
		int z2 = Random.Range(0, z + 1);
		int x3 = x - x2;
		int z3 = z - z2;

		list.Add(new WidgetZone(zone.Parent, zone.X + x2, zone.Z + z2, zone.NumX, zone.NumZ, zone.SizeX - x, zone.SizeZ - z));
		if (x2 > 0) list.Add(new WidgetZone(zone.Parent, zone.X, zone.Z, zone.NumX, zone.NumZ, x2, zone.SizeZ));
		if (z2 > 0) list.Add(new WidgetZone(zone.Parent, zone.X + x2, zone.Z, zone.NumX, zone.NumZ, zone.SizeX - x, z2));
		if (x3 > 0) list.Add(new WidgetZone(zone.Parent, zone.X + zone.SizeX - x + x2, zone.Z, zone.NumX, zone.NumZ, x - x2, zone.SizeZ));
		if (z3 > 0) list.Add(new WidgetZone(zone.Parent, zone.X + x2, zone.Z + zone.SizeZ - z + z2, zone.NumX, zone.NumZ, zone.SizeX - x, z - z2));
		return list;
	}

	public static WidgetZone GetZone(List<WidgetZone> zones, Widget widget)
	{
		List<WidgetZone> list = new List<WidgetZone>();
		foreach (WidgetZone widgetZone in zones)
		{
			if (widget.SizeX <= widgetZone.SizeX && widget.SizeZ <= widgetZone.SizeZ)
			{
				list.Add(widgetZone);
			}
		}
		if (list.Count == 0)
		{
			return null;
		}
		return list[Random.Range(0, list.Count)];
	}

	public GameObject Parent;
	public int SizeX;
	public int SizeZ;
	public int X;
	public int Z;
	public int NumX;
	public int NumZ;
}