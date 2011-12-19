//#pragma warning disable 1591
using System;
using System.Collections.Generic;

namespace Rhino.Geometry
{
  /// <summary>
  /// Represents event data that is passed when when an item that meets certain 
  /// criteria is found and the passed RTree event is raised.
  /// </summary>
  public class RTreeEventArgs : EventArgs
  {
    IntPtr m_element_a;
    IntPtr m_element_b = IntPtr.Zero;
    bool m_bCancel = false;
    internal RTreeEventArgs(IntPtr a)
    {
      m_element_a = a;
    }
    internal RTreeEventArgs(IntPtr a, IntPtr b)
    {
      m_element_a = a;
      m_element_b = b;
    }

    /// <summary>
    /// Gets the identifier of the found item.
    /// </summary>
    /// <exception cref="System.OverflowException">If, on 64-bit platforms, the value of this instance is too large or too small to be represented as a 32-bit signed integer.</exception>
    public int Id { get { return m_element_a.ToInt32(); } }

    /// <summary>
    /// Gets the identifier pointer of the found item.
    /// </summary>
    public IntPtr IdPtr { get { return m_element_a; } }

    /// <summary>
    /// Gets or sets a value that determines if the search should be conducted farther.
    /// </summary>
    public bool Cancel
    {
      get { return m_bCancel; }
      set { m_bCancel = value; }
    }

    /// <summary>
    /// If search is using two r-trees, IdB is element b in the search.
    /// </summary>
    public int IdB { get { return m_element_b.ToInt32(); } }

    /// <summary>
    /// If search is using two r-trees, IdB is the element b pointer in the search.
    /// </summary>
    public IntPtr IdBPtr { get { return m_element_b; } }

    /// <summary>
    /// Gets or sets an arbitrary object that can be attached to this event args.
    /// This object will "stick" through a single search and can represent user-defined state.
    /// </summary>
    public object Tag { get; set; }
  }

  /// <summary>
  /// Represents a spatial search structure based on implementations of the
  /// R-tree algorithm by Toni Gutman.
  /// </summary>
  /// <remarks>
  /// The opennurbs rtree code is a modifed version of the free and unrestricted
  /// R-tree implementation obtianed from http://www.superliminal.com/sources/sources.htm
  /// </remarks>
  public class RTree : IDisposable
  {
    IntPtr m_ptr; //ON_rTree* - this class is never const
    long m_memory_pressure = 0;
    int m_count = -1;

    /// <summary>
    /// Initializes a new, empty instance of the tree.
    /// </summary>
    public RTree()
    {
      m_ptr = UnsafeNativeMethods.ON_RTree_New();
    }

    /// <summary>
    /// Creates a new tree with an element for each face in the mesh.
    /// The element id is set to the index of the face.
    /// </summary>
    /// <param name="mesh">A mesh.</param>
    /// <returns>A new tree, or null on error.</returns>
    public static RTree CreateMeshFaceTree(Mesh mesh)
    {
      RTree rc = new RTree();
      IntPtr pRtree = rc.NonConstPointer();
      IntPtr pConstMesh = mesh.ConstPointer();
      if (!UnsafeNativeMethods.ON_RTree_CreateMeshFaceTree(pRtree, pConstMesh))
      {
        rc.Dispose();
        rc = null;
      }
      uint size = UnsafeNativeMethods.ON_RTree_SizeOf(pRtree);
      rc.m_memory_pressure = size;
      GC.AddMemoryPressure(rc.m_memory_pressure);
      return rc;
    }

    /// <summary>
    /// Creates a new tree with an element for each pointcloud point.
    /// </summary>
    /// <param name="cloud">A pointcloud.</param>
    /// <returns>A new tree, or null on error.</returns>
    public static RTree CreatePointCloudTree(PointCloud cloud)
    {
      RTree rc = new RTree();
      IntPtr pRtree = rc.NonConstPointer();
      IntPtr pConstCloud = cloud.ConstPointer();
      if (!UnsafeNativeMethods.ON_RTree_CreatePointCloudTree(pRtree, pConstCloud))
      {
        rc.Dispose();
        rc = null;
      }
      uint size = UnsafeNativeMethods.ON_RTree_SizeOf(pRtree);
      rc.m_memory_pressure = size;
      GC.AddMemoryPressure(rc.m_memory_pressure);
      return rc;

    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(Point3d point, int elementId)
    {
      return Insert(new BoundingBox(point, point), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(Point3d point, IntPtr elementId)
    {
      return Insert(new BoundingBox(point, point), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(BoundingBox box, int elementId)
    {
      return Insert(box, new IntPtr(elementId));
    }

    /// <summary>Insert an element into the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(BoundingBox box, IntPtr elementId)
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_RTree_InsertRemove(pThis, true, box.Min, box.Max, elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(Point2d point, int elementId)
    {
      return Insert(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>Inserts an element into the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully inserted.</returns>
    public bool Insert(Point2d point, IntPtr elementId)
    {
      return Insert(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed.</returns>
    public bool Remove(Point3d point, int elementId)
    {
      return Remove(new BoundingBox(point, point), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully removed.</returns>
    public bool Remove(Point3d point, IntPtr elementId)
    {
      return Remove(new BoundingBox(point, point), elementId);
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed.</returns>
    public bool Remove(BoundingBox box, int elementId)
    {
      return Remove(box, new IntPtr(elementId));
    }

    /// <summary>Removes an element from the tree.</summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="elementId">A pointer.</param>
    /// <returns>true if element was successfully removed.</returns>
    public bool Remove(BoundingBox box, IntPtr elementId)
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      return UnsafeNativeMethods.ON_RTree_InsertRemove(pThis, false, box.Min, box.Max, elementId);
    }

    /// <summary>Removes an element from the tree</summary>
    /// <param name="point">A point.</param>
    /// <param name="elementId">A number.</param>
    /// <returns>true if element was successfully removed</returns>
    public bool Remove(Point2d point, int elementId)
    {
      return Remove(new Point3d(point.X, point.Y, 0), elementId);
    }

    /// <summary>
    /// Removes all elements.
    /// </summary>
    public void Clear()
    {
      m_count = -1; 
      IntPtr pThis = NonConstPointer();
      UnsafeNativeMethods.ON_RTree_RemoveAll(pThis);
    }

    /// <summary>
    /// Gets the number of items in this tree.
    /// </summary>
    public int Count
    {
      get
      {
        if (m_count < 0)
        {
          IntPtr pThis = NonConstPointer();
          m_count = UnsafeNativeMethods.ON_RTree_ElementCount(pThis);
        }
        return m_count;
      }
    }
    
    static int m_next_serial_number = 1;
    class Callbackholder
    {
      public RTree Sender { get; set; }
      public int SerialNumber { get; set; }
      public EventHandler<RTreeEventArgs> Callback { get; set; }
      public object Tag { get; set; }
    }
    static List<Callbackholder> m_callbacks;

    internal delegate int SearchCallback(int serial_number, IntPtr idA, IntPtr idB);
    private static int CustomSearchCallback(int serial_number, IntPtr idA, IntPtr idB)
    {
      Callbackholder cbh = null;
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        Callbackholder holder = m_callbacks[i];
        if (holder.SerialNumber == serial_number)
        {
          cbh = holder;
          break;
        }
      }
      int rc = 1;
      if (cbh != null)
      {
        RTreeEventArgs e = new RTreeEventArgs(idA, idB);
        e.Tag = cbh.Tag;
        cbh.Callback(cbh.Sender, e);
        if (e.Cancel)
          rc = 0;
        cbh.Tag = e.Tag;
      }
      return rc;
    }

    /// <summary>
    /// Searches for items in a bounding box.
    /// <para>The bounding box can be singular and contain exactly one single point.</para>
    /// </summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback)
    {
      return Search(box, callback, null);
    }

    /// <summary>
    /// Searches for items in a bounding box.
    /// <para>The bounding box can be singular and contain exactly one single point.</para>
    /// </summary>
    /// <param name="box">A bounding box.</param>
    /// <param name="callback">An event handler to be raised when items are found.</param>
    /// <param name="tag">State to be passed inside the <see cref="RTreeEventArgs"/> Tag property.</param>
    /// <returns>
    /// true if entire tree was searched. It is possible no results were found.
    /// </returns>
    public bool Search(BoundingBox box, EventHandler<RTreeEventArgs> callback, object tag)
    {
      IntPtr pConstTree = ConstPointer();
      if (m_callbacks == null)
        m_callbacks = new List<Callbackholder>();
      Callbackholder cbh = new Callbackholder();
      cbh.SerialNumber = m_next_serial_number++;
      cbh.Callback = callback;
      cbh.Sender = this;
      cbh.Tag = tag;
      m_callbacks.Add(cbh);
      SearchCallback searcher = CustomSearchCallback;
      bool rc = UnsafeNativeMethods.ON_RTree_Search(pConstTree, box.Min, box.Max, cbh.SerialNumber, searcher);
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        if (m_callbacks[i].SerialNumber == cbh.SerialNumber)
        {
          m_callbacks.RemoveAt(i);
          break;
        }
      }
      return rc;
    }

    /// <summary>
    /// Searches two R-trees for all pairs elements whose bounding boxes overlap.
    /// </summary>
    /// <param name="treeA">A first tree.</param>
    /// <param name="treeB">A second tree.</param>
    /// <param name="tolerance">
    /// If the distance between a pair of bounding boxes is less than tolerance,
    /// then callback is called.
    /// </param>
    /// <param name="callback">A callback event handler.</param>
    /// <returns>
    /// True if entire tree was searched.  It is possible no results were found.
    /// </returns>
    public static bool SearchOverlaps(RTree treeA, RTree treeB, double tolerance, EventHandler<RTreeEventArgs> callback)
    {
      IntPtr pConstTreeA = treeA.ConstPointer();
      IntPtr pConstTreeB = treeB.ConstPointer();
      if (m_callbacks == null)
        m_callbacks = new List<Callbackholder>();
      Callbackholder cbh = new Callbackholder();
      cbh.SerialNumber = m_next_serial_number++;
      cbh.Callback = callback;
      cbh.Sender = null;
      m_callbacks.Add(cbh);
      SearchCallback searcher = CustomSearchCallback;
      bool rc = UnsafeNativeMethods.ON_RTree_Search2(pConstTreeA, pConstTreeB, tolerance, cbh.SerialNumber, searcher);
      for (int i = 0; i < m_callbacks.Count; i++)
      {
        if (m_callbacks[i].SerialNumber == cbh.SerialNumber)
        {
          m_callbacks.RemoveAt(i);
          break;
        }
      }
      return rc;
    }

    #region pointer / disposable handlers
    IntPtr ConstPointer() { return m_ptr; }
    IntPtr NonConstPointer() { return m_ptr; }

    /// <summary>
    /// Passively reclaims unmanaged resources when the class user did not explicitly call Dispose().
    /// </summary>
    ~RTree()
    {
      Dispose(false);
    }

    /// <summary>
    /// Actively reclaims unmanaged resources that this instance uses.
    /// </summary>
    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    /// <summary>
    /// For derived class implementers.
    /// <para>This method is called with argument true when class user calls Dispose(), while with argument false when
    /// the Garbage Collector invokes the finalizer, or Finalize() method.</para>
    /// <para>You must reclaim all used unmanaged resources in both cases, and can use this chance to call Dispose on disposable fields if the argument is true.</para>
    /// <para>Also, you must call the base virtual method within your overriding method.</para>
    /// </summary>
    /// <param name="disposing">true if the call comes from the Dispose() method; false if it comes from the Garbage Collector finalizer.</param>
    protected virtual void Dispose(bool disposing)
    {
      if (IntPtr.Zero != m_ptr)
      {
        UnsafeNativeMethods.ON_RTree_Delete(m_ptr);
        m_ptr = IntPtr.Zero;
      }
      if (m_memory_pressure > 0)
      {
        GC.RemoveMemoryPressure(m_memory_pressure);
        m_memory_pressure = 0;
      }
    }
    #endregion

  }
}
