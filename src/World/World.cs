#nullable enable
using System.Collections.Generic;
using ZenECS.Core.World.Internal;

namespace ZenECS.Core.World
{
   public partial class World : IWorld
   {
      public WorldId Id { get; }
      public string?  Name { get; set; }
      public IReadOnlyCollection<string>? Tags { get; }
      public bool IsPaused { get; set; }
      
      private readonly IWorldInternal? _internal;
      
      internal World(IWorldInternal impl)
      {
         _internal = impl;
         //_store = _internal.GetRequired<IEntityStore>();
      }
   }
}