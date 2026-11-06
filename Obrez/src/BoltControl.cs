namespace HalfGripObrez
{
    internal static class BoltControl
    {
        // Shared with NativeBoltTests, which exercises these settings on the actual
        // game's LinearObject rather than a second implementation of bolt motion.
        internal static void Configure(LinearObject bolt)
        {
            bolt.grabRedirect = null;
            bolt.frozen = false;
            bolt.isPhysicsControlled = false;
            bolt.objectSpringForce = 0f;
            bolt.physicsObjectSpringForce = 0f;
            bolt.directSpeedKeptOnRelease = 0f;
        }
    }
}
