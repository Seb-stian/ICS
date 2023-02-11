﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CookBook.BL.Mappers;
using CookBook.BL.Models;
using CookBook.DAL.Entities;
using CookBook.DAL.Mappers;
using CookBook.DAL.Repositories;
using CookBook.DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace CookBook.BL.Facades;

public abstract class
    FacadeBase<TEntity, TListModel, TDetailModel, TEntityMapper> : IFacade<TEntity, TListModel, TDetailModel>
    where TEntity : class, IEntity
    where TListModel : IModel
    where TDetailModel : class, IModel
    where TEntityMapper : IEntityMapper<TEntity>, new()
{
    protected readonly IModelMapper<TEntity, TListModel, TDetailModel> ModelMapper;
    protected readonly IUnitOfWorkFactory UnitOfWorkFactory;

    protected FacadeBase(
        IUnitOfWorkFactory unitOfWorkFactory,
        IModelMapper<TEntity, TListModel, TDetailModel> modelMapper)
    {
        UnitOfWorkFactory = unitOfWorkFactory;
        ModelMapper = modelMapper;
    }

    protected virtual string IncludesNavigationPathDetail => string.Empty;

    public async Task DeleteAsync(Guid id)
    {
        await using IUnitOfWork uow = UnitOfWorkFactory.Create();
        uow.GetRepository<TEntity, TEntityMapper>().Delete(id);
        await uow.CommitAsync().ConfigureAwait(false);
    }

    public virtual async Task<TDetailModel?> GetAsync(Guid id)
    {
        await using IUnitOfWork uow = UnitOfWorkFactory.Create();

        IQueryable<TEntity> query = uow.GetRepository<TEntity, TEntityMapper>().Get();

        if (string.IsNullOrWhiteSpace(IncludesNavigationPathDetail) is false)
        {
            query = query.Include(IncludesNavigationPathDetail);
        }

        TEntity? entity = await query.SingleOrDefaultAsync(e => e.Id == id);

        return entity is null
            ? null
            : ModelMapper.MapToDetailModel(entity);
    }

    public virtual async Task<IEnumerable<TListModel>> GetAsync()
    {
        await using IUnitOfWork uow = UnitOfWorkFactory.Create();
        List<TEntity> entities = uow
            .GetRepository<TEntity, TEntityMapper>()
            .Get()
            .ToList();

        return ModelMapper.MapToListModel(entities);
    }

    public virtual async Task<TDetailModel> SaveAsync(TDetailModel model)
    {
        TDetailModel result;

        TEntity entity = ModelMapper.MapToEntity(model);

        IUnitOfWork uow = UnitOfWorkFactory.Create();
        IRepository<TEntity> repository = uow.GetRepository<TEntity, TEntityMapper>();

        if (await repository.ExistsAsync(entity))
        {
            TEntity updatedEntity = await repository.UpdateAsync(entity);
            result = ModelMapper.MapToDetailModel(updatedEntity);
        }
        else
        {
            entity.Id = Guid.NewGuid();
            TEntity insertedEntity = await repository.InsertAsync(entity);
            result = ModelMapper.MapToDetailModel(insertedEntity);
        }

        await uow.CommitAsync();

        return result;
    }
}
