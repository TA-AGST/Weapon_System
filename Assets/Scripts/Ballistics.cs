using UnityEngine;

// Bullet drop maths. Used to zero the gun and to place the hold-over marks inside the scope.
public static class Ballistics
{
    public const float Gravity = 9.81f;   // metres per second squared, as a positive number

    // How far (radians) the barrel must point above the line of sight so the bullet
    // crosses the line of sight at 'distance'. This is what "zeroing" a gun means.
    public static float ZeroAngle(float speed, float distance)
    {
        float angle = 0f;
        for (int i = 0; i < 4; i++)
        {
            float cos = Mathf.Cos(angle);
            angle = Mathf.Atan(Gravity * distance / (2f * speed * speed * cos * cos));
        }
        return angle;
    }

    // Extra angle (radians) you have to aim ABOVE a target at 'distance' when the gun is zeroed at 'zeroDistance'.
    // The scope marks are drawn this far below the centre, so you put the mark on the target.
    public static float HoldoverAngle(float speed, float zeroDistance, float distance)
    {
        return ZeroAngle(speed, distance) - ZeroAngle(speed, zeroDistance);
    }
}
