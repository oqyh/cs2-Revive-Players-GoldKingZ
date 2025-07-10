using System;
using System.Runtime.InteropServices;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using System.Runtime.CompilerServices;
using System.Numerics;
using CSSVector = CounterStrikeSharp.API.Modules.Utils.Vector;
using CSSQAngle = CounterStrikeSharp.API.Modules.Utils.QAngle;


namespace Revive_Players;

[StructLayout(LayoutKind.Sequential)]
public struct My_Vector : IAdditionOperators<My_Vector, My_Vector, My_Vector>,
    ISubtractionOperators<My_Vector, My_Vector, My_Vector>,
    IMultiplyOperators<My_Vector, float, My_Vector>,
    IDivisionOperators<My_Vector, float, My_Vector>
{
    public float X, Y, Z;

    public My_Vector(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public void Scale(float scalar)
    {
        X *= scalar;
        Y *= scalar;
        Z *= scalar;
    }

    public void Add(in My_Vector other)
    {
        X += other.X;
        Y += other.Y;
        Z += other.Z;
    }

    public readonly override string ToString() => $"{X:n2} {Y:n2} {Z:n2}";

    public static My_Vector operator +(My_Vector left, My_Vector right) => new(
        left.X + right.X,
        left.Y + right.Y,
        left.Z + right.Z
    );

    public static My_Vector operator -(My_Vector left, My_Vector right) => new(
        left.X - right.X,
        left.Y - right.Y,
        left.Z - right.Z
    );

    public static My_Vector operator -(My_Vector value) => new(-value.X, -value.Y, -value.Z);

    public static My_Vector operator *(My_Vector left, float right) => new(
        left.X * right,
        left.Y * right,
        left.Z * right
    );

    public static My_Vector operator /(My_Vector left, float right) => new(
        left.X / right,
        left.Y / right,
        left.Z / right
    );
}

[StructLayout(LayoutKind.Sequential)]
public struct My_QAngle : IAdditionOperators<My_QAngle, My_QAngle, My_QAngle>,
    ISubtractionOperators<My_QAngle, My_QAngle, My_QAngle>,
    IMultiplyOperators<My_QAngle, float, My_QAngle>,
    IDivisionOperators<My_QAngle, float, My_QAngle>
{
    public float X, Y, Z;

    public My_QAngle(float x, float y, float z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public unsafe (My_Vector fwd, My_Vector right, My_Vector up) AngleVectors()
    {
        My_Vector fwd = default, right = default, up = default;
        fixed (My_QAngle* pThis = &this)
        {
            NativeAPI.AngleVectors((IntPtr)pThis, (IntPtr)(&fwd), (IntPtr)(&right), (IntPtr)(&up));
        }
        return (fwd, right, up);
    }

    public readonly override string ToString() => $"{X:n2} {Y:n2} {Z:n2}";

    public static My_QAngle operator +(My_QAngle left, My_QAngle right) => new(
        left.X + right.X,
        left.Y + right.Y,
        left.Z + right.Z
    );

    public static My_QAngle operator -(My_QAngle left, My_QAngle right) => new(
        left.X - right.X,
        left.Y - right.Y,
        left.Z - right.Z
    );

    public static My_QAngle operator -(My_QAngle value) => new(-value.X, -value.Y, -value.Z);

    public static My_QAngle operator *(My_QAngle left, float right) => new(
        left.X * right,
        left.Y * right,
        left.Z * right
    );

    public static My_QAngle operator /(My_QAngle left, float right) => new(
        left.X / right,
        left.Y / right,
        left.Z / right
    );
}

public static unsafe class VectorExtensions
{
    public static void My_Teleport(
        this CBaseEntity entity,
        My_Vector? position = null,
        My_QAngle? angles = null,
        My_Vector? velocity = null
    )
    {
        if (entity == null || !entity.IsValid)
            return;

        var teleport = VirtualFunction.CreateVoid<IntPtr, IntPtr, IntPtr, IntPtr>(
            entity.Handle,
            GameData.GetOffset("CBaseEntity_Teleport")
        );

        My_Vector posCopy = default;
        My_QAngle angCopy = default;
        My_Vector velCopy = default;

        IntPtr pPos = IntPtr.Zero;
        IntPtr pAng = IntPtr.Zero;
        IntPtr pVel = IntPtr.Zero;

        if (position.HasValue)
        {
            posCopy = position.Value;
            pPos = (IntPtr)Unsafe.AsPointer(ref posCopy);
        }

        if (angles.HasValue)
        {
            angCopy = angles.Value;
            pAng = (IntPtr)Unsafe.AsPointer(ref angCopy);
        }

        if (velocity.HasValue)
        {
            velCopy = velocity.Value;
            pVel = (IntPtr)Unsafe.AsPointer(ref velCopy);
        }

        teleport(entity.Handle, pPos, pAng, pVel);
    }

    public static My_Vector ToMy_Vector(this CSSVector vec) => new(vec.X, vec.Y, vec.Z);

    public static My_QAngle ToMy_QAngle(this CSSQAngle ang) => new(ang.X, ang.Y, ang.Z);

    public static CSSVector ToManagedVector(this My_Vector vec) => new CSSVector(vec.X, vec.Y, vec.Z);
}