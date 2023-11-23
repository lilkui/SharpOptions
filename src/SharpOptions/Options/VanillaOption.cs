namespace SharpOptions.Options;

public class VanillaOption : Option
{
    public required OptionType OptionType { get; set; }

    public required double Strike { get; set; }

    public required ExerciseType ExerciseType { get; set; }
}