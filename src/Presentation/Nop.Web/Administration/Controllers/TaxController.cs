﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Routing;
using Nop.Admin.Models;
using Nop.Admin.Models.Tax;
using Nop.Core;
using Nop.Core.Domain.Common;
using Nop.Core.Domain.Directory;
using Nop.Core.Domain.Tax;
using Nop.Services.Common;
using Nop.Services.Configuration;
using Nop.Services.Directory;
using Nop.Services.Tax;
using Nop.Web.Framework;
using Nop.Web.Framework.Controllers;
using Telerik.Web.Mvc;

namespace Nop.Admin.Controllers
{
	[AdminAuthorize]
    public class TaxController : BaseNopController
	{
		#region Fields

        private readonly ITaxService _taxService;
        private readonly ITaxCategoryService _taxCategoryService;
        private TaxSettings _taxSettings;
        private readonly ISettingService _settingService;

	    #endregion

		#region Constructors

        public TaxController(ITaxService taxService,
            ITaxCategoryService taxCategoryService, TaxSettings taxSettings,
            ISettingService settingService)
		{
            this._taxService = taxService;
            this._taxCategoryService = taxCategoryService;
            this._taxSettings = taxSettings;
            this._settingService = settingService;
		}

		#endregion Constructors 

        #region Tax Providers

        public ActionResult Providers(string systemName)
        {
            //mark as active tax provider (if selected)
            if (!String.IsNullOrEmpty(systemName))
            {
                var taxProvider = _taxService.LoadTaxProviderBySystemName(systemName);
                if (taxProvider != null)
                {
                    _taxSettings.ActiveTaxProviderSystemName = systemName;
                    _settingService.SaveSetting(_taxSettings);
                }
            }

            var taxProvidersModel = _taxService.LoadAllTaxProviders()
                .Select(x => x.ToModel()).ToList();
            foreach (var tpm in taxProvidersModel)
                tpm.IsPrimaryTaxProvider = tpm.SystemName.Equals(_taxSettings.ActiveTaxProviderSystemName, StringComparison.InvariantCultureIgnoreCase);
            var gridModel = new GridModel<TaxProviderModel>
            {
                Data = taxProvidersModel,
                Total = taxProvidersModel.Count()
            };
            return View(gridModel);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult Providers(GridCommand command)
        {
            var taxProvidersModel = _taxService.LoadAllTaxProviders()
                .Select(x => x.ToModel())
                .ForCommand(command)
                .ToList();
            foreach (var tpm in taxProvidersModel)
                tpm.IsPrimaryTaxProvider = tpm.SystemName.Equals(_taxSettings.ActiveTaxProviderSystemName, StringComparison.InvariantCultureIgnoreCase);
            var gridModel = new GridModel<TaxProviderModel>
            {
                Data = taxProvidersModel,
                Total = taxProvidersModel.Count()
            };
            return new JsonResult
            {
                Data = gridModel
            };
        }

        public ActionResult ConfigureProvider(string systemName)
        {
            var taxProvider = _taxService.LoadTaxProviderBySystemName(systemName);
            if (taxProvider == null) throw new ArgumentException("No tax provider found with the specified system name", "systemName");

            var model = taxProvider.ToModel();
            string actionName, controllerName;
            RouteValueDictionary routeValues;
            taxProvider.GetConfigurationRoute(out actionName, out controllerName, out routeValues);
            model.ConfigurationActionName = actionName;
            model.ConfigurationControllerName = controllerName;
            model.ConfigurationRouteValues = routeValues;
            return View(model);
        }

        #endregion

        #region Tax categories

        public ActionResult Categories()
        {
            var categoriesModel = _taxCategoryService.GetAllTaxCategories()
                .Select(x => x.ToModel())
                .ToList();
            var model = new GridModel<TaxCategoryModel>
            {
                Data = categoriesModel,
                Total = categoriesModel.Count
            };
            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult Categories(GridCommand command)
        {
            var categoriesModel = _taxCategoryService.GetAllTaxCategories()
                .Select(x => x.ToModel())
                .ForCommand(command)
                .ToList();
            var model = new GridModel<TaxCategoryModel>
            {
                Data = categoriesModel,
                Total = categoriesModel.Count
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryUpdate(TaxCategoryModel model, GridCommand command)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("Categories");
            }

            var taxCategory = _taxCategoryService.GetTaxCategoryById(model.Id);
            taxCategory = model.ToEntity(taxCategory);
            _taxCategoryService.UpdateTaxCategory(taxCategory);

            return Categories(command);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryAdd([Bind(Exclude = "Id")] TaxCategoryModel model, GridCommand command)
        {
            if (!ModelState.IsValid)
            {
                return new JsonResult { Data = "error" };
            }

            var taxCategory = new TaxCategory();
            taxCategory = model.ToEntity(taxCategory);
            _taxCategoryService.InsertTaxCategory(taxCategory);

            return Categories(command);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult CategoryDelete(int id, GridCommand command)
        {
            var taxCategory = _taxCategoryService.GetTaxCategoryById(id);
            _taxCategoryService.DeleteTaxCategory(taxCategory);

            return Categories(command);
        }

        #endregion
    }
}
