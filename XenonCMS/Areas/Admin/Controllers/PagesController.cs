﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using XenonCMS.Areas.Admin.ViewModels.Pages;
using XenonCMS.Classes;
using XenonCMS.Models;

namespace XenonCMS.Areas.Admin.Controllers
{
    [Authorize(Roles = "GlobalAdmin, SiteAdmin")]
    public class PagesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: /Admin/Pages/
        public ActionResult Index()
        {
            string RequestDomain = Globals.GetRequestDomain();
            return View(ModelConverter.Convert<Index>(db.SitePages.Where(x => x.Site.Domain == RequestDomain).OrderBy(x => x.ParentId).ThenBy(x => x.DisplayOrder).ToArray()));
        }

        // GET: /Admin/Pages/Create
        public ActionResult Create()
        {
            var ViewModel = new Create();
            ViewModel.GetParents(db);
            return View(ViewModel);
        }

        // POST: /Admin/Pages/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(Create viewModel)
        {
            // TODO Ensure slug isn't equal to the name of any controllers, or does not start with <controller>/something

            if (ModelState.IsValid)
            {
                string RequestDomain = Globals.GetRequestDomain();

                // Ensure slug is unique
                string Slug = Globals.GetSlug(viewModel.Slug, true);
                if (db.SitePages.Any(x => (x.Site.Domain == RequestDomain) && (x.Slug == Slug)))
                {
                    ModelState.AddModelError("SlugAlreadyUsed", "Slug has already been used");
                    viewModel.GetParents(db);
                    return View(viewModel);
                }
                else
                {
                    SitePage NewPage = ModelConverter.Convert<SitePage>(viewModel);

                    // Assign values for fields not on form
                    NewPage.DateAdded = DateTime.Now;
                    NewPage.DateLastUpdated = DateTime.Now;
                    NewPage.SiteId = db.Sites.Single(x => x.Domain == RequestDomain).Id;

                    // Transform values
                    NewPage.Html = Globals.SaveImagesToDisk(NewPage.Html);
                    if (NewPage.ParentId <= 0) NewPage.ParentId = null;
                    NewPage.Slug = Globals.GetSlug(NewPage.Slug, true);

                    // Save changes
                    db.SitePages.Add(NewPage);
                    db.SaveChanges();

                    // Update cache
                    Caching.ResetPages();

                    return RedirectToAction("Index");
                }
            }
            else
            {
                viewModel.GetParents(db);
                return View(viewModel);
            }
        }

        // GET: /Admin/Pages/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                string RequestDomain = Globals.GetRequestDomain();
                SitePage Page = db.SitePages.SingleOrDefault(x => (x.Id == id) && (x.Site.Domain == RequestDomain));
                if (Page == null)
                {
                    return HttpNotFound();
                }
                else
                {
                    var ViewModel = ModelConverter.Convert<Edit>(Page);
                    ViewModel.GetParents(db);
                    return View(ViewModel);
                }
            }
        }

        // POST: /Admin/Pages/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(Edit viewModel)
        {
            // TODO Ensure slug isn't equal to the name of any controllers, or does not start with <controller>/something

            if (ModelState.IsValid)
            {
                string RequestDomain = Globals.GetRequestDomain();

                SitePage EditedPage = db.SitePages.SingleOrDefault(x => (x.Id == viewModel.Id) && (x.Site.Domain == RequestDomain));
                if (EditedPage == null)
                {
                    return HttpNotFound();
                }
                else
                {
                    // Ensure slug is unique
                    string OldSlug = EditedPage.Slug;
                    string NewSlug = Globals.GetSlug(viewModel.Slug, true);
                    if ((OldSlug != NewSlug) && (db.SitePages.Any(x => (x.Site.Domain == RequestDomain) && (x.Slug == NewSlug))))
                    {
                        ModelState.AddModelError("SlugAlreadyUsed", "Slug has already been used");
                        viewModel.GetParents(db);
                        return View(viewModel);
                    }
                    else
                    {
                        // View model to domain model
                        ModelConverter.Convert(viewModel, EditedPage);

                        // Assign values for fields not on form
                        EditedPage.DateLastUpdated = DateTime.Now;

                        // Transform values
                        EditedPage.Html = Globals.SaveImagesToDisk(EditedPage.Html);
                        if (EditedPage.ParentId <= 0) EditedPage.ParentId = null;
                        EditedPage.Slug = NewSlug;

                        // Save changes
                        db.Entry(EditedPage).State = EntityState.Modified;
                        db.SaveChanges();

                        // Update cache
                        Caching.ResetPages();

                        return RedirectToAction("Index");
                    }
                }
            }
            else
            {
                viewModel.GetParents(db);
                return View(viewModel);
            }
        }

        // GET: /Admin/Pages/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            else
            {
                string RequestDomain = Globals.GetRequestDomain();
                SitePage Page = db.SitePages.SingleOrDefault(x => (x.Id == id) && (x.Site.Domain == RequestDomain));
                if (Page == null)
                {
                    return HttpNotFound();
                }
                else
                {
                    return View(ModelConverter.Convert<Delete>(Page));
                }
            }
        }

        // POST: /Admin/Pages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            string RequestDomain = Globals.GetRequestDomain();
            SitePage Page = db.SitePages.SingleOrDefault(x => (x.Id == id) && (x.Site.Domain == RequestDomain));
            if (Page == null)
            {
                return HttpNotFound();
            }
            else
            {
                // Update cache
                Caching.ResetPages();

                // Save changes
                db.SitePages.Remove(Page);
                db.SaveChanges();

                return RedirectToAction("Index");
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
