using VirtoCommerce.ExportModule.Data.Services;

namespace VirtoCommerce.ExportModule.Tests.ComplexExportPagedDataSourceTests.Mocks
{
    public class ComplexExportPagedDataSource : ComplexExportPagedDataSource<ComplexExportDataQuery>
    {
        private readonly IComplexDataSearchService _service1;
        private readonly IComplexDataSearchService _service2;
        private readonly IComplexDataSearchService _service3;
        private readonly IComplexDataSearchService _service4;
        private readonly IComplexDataSearchService _service5;

        public ComplexExportPagedDataSource(IComplexDataSearchService service1,
            IComplexDataSearchService service2,
            IComplexDataSearchService service3,
            IComplexDataSearchService service4,
            IComplexDataSearchService service5,
            ComplexExportDataQuery dataQuery)
        : base(dataQuery)
        {
            _service1 = service1;
            _service2 = service2;
            _service3 = service3;
            _service4 = service4;
            _service5 = service5;
        }

        protected override void InitDataSourceStates()
        {
            _exportDataSourceStates.AddRange(new ExportDataSourceState[]
            {
                new ExportDataSourceState
                {
                    FetchFunc = async (x) =>
                    {
                        var searchResult = await _service1.SearchAsync(new ComplexSearchCriteria{Skip =x.Skip, Take = x.Take});
                        x.TotalCount = searchResult.TotalCount;
                        x.Result = searchResult.Results;
                    }
                },
                new ExportDataSourceState
                {
                    FetchFunc = async (x) =>
                    {
                        var searchResult = await _service2.SearchAsync(new ComplexSearchCriteria{Skip =x.Skip, Take = x.Take});
                        x.TotalCount = searchResult.TotalCount;
                        x.Result = searchResult.Results;
                    }
                },
                new ExportDataSourceState
                {
                    FetchFunc = async (x) =>
                    {
                        var searchResult = await _service3.SearchAsync(new ComplexSearchCriteria{Skip =x.Skip, Take = x.Take});
                        x.TotalCount = searchResult.TotalCount;
                        x.Result = searchResult.Results;
                    }
                },
                new ExportDataSourceState
                {
                    FetchFunc = async (x) =>
                    {
                        var searchResult = await _service4.SearchAsync(new ComplexSearchCriteria{Skip =x.Skip, Take = x.Take});
                        x.TotalCount = searchResult.TotalCount;
                        x.Result = searchResult.Results;
                    }
                },
                new ExportDataSourceState
                {
                    FetchFunc = async (x) =>
                    {
                        var searchResult = await _service5.SearchAsync(new ComplexSearchCriteria{Skip =x.Skip, Take = x.Take});
                        x.TotalCount = searchResult.TotalCount;
                        x.Result = searchResult.Results;
                    }
                },
            });
        }
    }
}
