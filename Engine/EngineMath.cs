namespace Engine
{
    /// <summary>
    /// Math is hard! Let us make it more accessible.
    /// </summary>
    public static class EngineMath
    {
        /// <summary>
        /// Using an epsilon is very common when comparing floating-point numbers (dealing with error margins):
        /// https://www.reddit.com/r/explainlikeimfive/comments/4ufw5t/eli5_within_epsilon_of_in_programming/
        /// </summary>
        public static float Epsilon { get; } = 0.00001f;

        // Units are in seconds or pixels (world units).
        public static float Sine(float elapsedTime, float amplitude, float period, float phaseShift = 0, float verticalShift = 0)
        {
            float frequency = 2 * MathF.PI / period;
            return amplitude * MathF.Sin(frequency * (elapsedTime - phaseShift)) + verticalShift;
        }

        public static float Cosine(float elapsedTime, float amplitude, float period, float phaseShift = 0, float verticalShift = 0)
        {
            float frequency = 2 * MathF.PI / period;
            return amplitude * MathF.Cos(frequency * (elapsedTime - phaseShift)) + verticalShift;
        }

        public static float Square(float elapsedTime, float amplitude, float period, float phaseShift = 0, float verticalShift = 0)
        {
            return amplitude * MathF.Sign(Cosine(elapsedTime, amplitude, period, phaseShift, verticalShift));
        }
    }
}
