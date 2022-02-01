namespace BMBF.Patching
{
    public enum OverwriteMode
    {
        /// <summary>
        /// The file must be newly created
        /// </summary>
        MustBeNew,

        /// <summary>
        /// The file can already exist, or it can be overwritten
        /// </summary>
        CanExist,

        /// <summary>
        /// The file must already exist, and will be overwritten
        /// </summary>
        MustExist
    }
}
