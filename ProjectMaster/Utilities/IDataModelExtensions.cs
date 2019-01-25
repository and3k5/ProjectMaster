using ProjectMaster.DataModels;

namespace ProjectMaster.Utilities
{
    public static class IDataModelExtensions
    {
        public static TOut ConvertToModel<TIn, TOut>(this TIn dataModel) where TIn : class, IDataModel where TOut : class, TIn, new()
        {
            var model = new TOut();
            ObjectUtility.CopyCommonValues(dataModel, model);
            return model;
        }

        public static TOut ConvertToDataModel<TIn, TOut>(this TIn model) where TOut : class, IDataModel, new() where TIn : class, TOut, IDataModel
        {
            var dataModel = new TOut();
            ObjectUtility.CopyCommonValues(model, dataModel);
            return dataModel;
        }
    }
}