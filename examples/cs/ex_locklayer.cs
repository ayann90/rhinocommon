﻿using Rhino;
using Rhino.Input;
using Rhino.Commands;
using System;
using System.Linq;

namespace examples_cs
{
  [System.Runtime.InteropServices.Guid("A77507C3-DEEE-4A2C-ADB3-3FFAF89B7EDD")]
  public class LockLayerCommand : Command
  {
    public override string EnglishName { get { return "csLockLayer"; } }

    protected override Result RunCommand(RhinoDoc doc, RunMode mode)
    {
      string layer_name = "";
      var rc = RhinoGet.GetString("Name of layer to lock", true, ref layer_name);
      if (rc != Result.Success)
        return rc;
      if (String.IsNullOrWhiteSpace(layer_name))
        return Result.Nothing;
     
      // because of sublayers it's possible that mone than one layer has the same name
      // so simply calling doc.Layers.Find(layerName) isn't good enough.  If "layerName" returns
      // more than one layer then present them to the user and let him decide.
      var matching_layers = (from layer in doc.Layers
                             where layer.Name == layer_name
                             select layer).ToList<Rhino.DocObjects.Layer>();

      Rhino.DocObjects.Layer layer_to_rename = null;
      if (matching_layers.Count == 0)
      {
        RhinoApp.WriteLine(String.Format("Layer \"{0}\" does not exist.", layer_name));
        return Result.Nothing;
      }
      else if (matching_layers.Count == 1)
      {
        layer_to_rename = matching_layers[0];
      }
      else if (matching_layers.Count > 1)
      {
        for (int i = 0; i < matching_layers.Count; i++)
        {
          RhinoApp.WriteLine(String.Format("({0}) {1}", i+1, matching_layers[i].FullPath.Replace("::", "->")));
        }
        int selected_layer = -1;
        rc = RhinoGet.GetInteger("which layer?", true, ref selected_layer);
        if (rc != Result.Success)
          return rc;
        if (selected_layer > 0 && selected_layer <= matching_layers.Count)
          layer_to_rename = matching_layers[selected_layer - 1];
        else return Result.Nothing;
      }

      if (layer_to_rename == null)
        return Result.Nothing;

      if (!layer_to_rename.IsLocked)
      {
        layer_to_rename.IsLocked = true;
        layer_to_rename.CommitChanges();
        return Result.Success;
      }
      else
      {
        RhinoApp.WriteLine(String.Format("layer {0} is already locked.", layer_to_rename.FullPath));
        return Result.Nothing;
      } 
    }
  }
}