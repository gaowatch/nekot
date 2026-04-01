using System;
using System.Threading;
using System.Threading.Tasks;

namespace NekoT.Core.Storage;

public interface IWriteBuffer<T> { void MarkDirty(T data); Task FlushAsync(); bool IsDirty { get; } DateTime LastFlushTime { get; } }