﻿using System.Data.Entity;

namespace Ardalis.Specification.EntityFramework6;

/// <inheritdoc/>
public abstract class RepositoryBase<T> : IRepositoryBase<T> where T : class
{
    protected DbContext DbContext { get; set; }
    protected ISpecificationEvaluator SpecificationEvaluator { get; set; }

    public RepositoryBase(DbContext dbContext)
        : this(dbContext, EntityFramework6.SpecificationEvaluator.Default)
    {
    }

    /// <inheritdoc/>
    public RepositoryBase(DbContext dbContext, ISpecificationEvaluator specificationEvaluator)
    {
        DbContext = dbContext;
        SpecificationEvaluator = specificationEvaluator;
    }

    /// <inheritdoc/>
    public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Add(entity);

        await SaveChangesAsync(cancellationToken);

        return entity;
    }

    /// <inheritdoc/>
    public virtual async Task<IEnumerable<T>> AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().AddRange(entities);

        await SaveChangesAsync(cancellationToken);

        return entities;
    }

    /// <inheritdoc/>
    public virtual async Task<int> UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Entry(entity).State = EntityState.Modified;

        var result = await SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<int> UpdateRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        foreach (var entity in entities)
        {
            DbContext.Entry(entity).State = EntityState.Modified;
        }

        var result = await SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<int> DeleteAsync(T entity, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().Remove(entity);

        var result = await SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<int> DeleteRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
    {
        DbContext.Set<T>().RemoveRange(entities);

        var result = await SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<int> DeleteRangeAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecification(specification);
        DbContext.Set<T>().RemoveRange(query);

        var result = await SaveChangesAsync(cancellationToken);
        return result;
    }

    /// <inheritdoc/>
    public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T> GetByIdAsync<TId>(TId id, CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().FindAsync(cancellationToken: cancellationToken, new object[] { id });
    }

    /// <inheritdoc/>
    public virtual async Task<T> FirstOrDefaultAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TResult> FirstOrDefaultAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).FirstOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<T> SingleOrDefaultAsync(ISingleResultSpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<TResult> SingleOrDefaultAsync<TResult>(ISingleResultSpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification).SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<List<T>> ListAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<List<T>> ListAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? queryResult
            : specification.PostProcessingAction(queryResult).AsList();
    }

    /// <inheritdoc/>
    public virtual async Task<List<TResult>> ListAsync<TResult>(ISpecification<T, TResult> specification, CancellationToken cancellationToken = default)
    {
        var queryResult = await ApplySpecification(specification).ToListAsync(cancellationToken);

        return specification.PostProcessingAction is null
            ? queryResult
            : specification.PostProcessingAction(queryResult).AsList();
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, true).CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyAsync(ISpecification<T> specification, CancellationToken cancellationToken = default)
    {
        return await ApplySpecification(specification, true).AnyAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public virtual async Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return await DbContext.Set<T>().AnyAsync(cancellationToken);
    }

#if NET8_0_OR_GREATER
    /// <inheritdoc/>
    public virtual IAsyncEnumerable<T> AsAsyncEnumerable(ISpecification<T> specification)
    {
        throw new NotImplementedException();
    }
#endif

    /// <summary>
    /// Filters the entities  of <typeparamref name="T"/>, to those that match the encapsulated query logic of the
    /// <paramref name="specification"/>.
    /// </summary>
    /// <param name="specification">The encapsulated query logic.</param>
    /// <param name="evaluateCriteriaOnly">It ignores pagination and evaluators that don't affect Count.</param>
    /// <returns>The filtered entities as an <see cref="IQueryable{T}"/>.</returns>
    protected virtual IQueryable<T> ApplySpecification(ISpecification<T> specification, bool evaluateCriteriaOnly = false)
    {
        return SpecificationEvaluator.GetQuery(DbContext.Set<T>().AsQueryable(), specification, evaluateCriteriaOnly);
    }

    /// <summary>
    /// Filters all entities of <typeparamref name="T" />, that matches the encapsulated query logic of the
    /// <paramref name="specification"/>, from the database.
    /// <para>
    /// Projects each entity into a new form, being <typeparamref name="TResult" />.
    /// </para>
    /// </summary>
    /// <typeparam name="TResult">The type of the value returned by the projection.</typeparam>
    /// <param name="specification">The encapsulated query logic.</param>
    /// <returns>The filtered projected entities as an <see cref="IQueryable{T}"/>.</returns>
    protected virtual IQueryable<TResult> ApplySpecification<TResult>(ISpecification<T, TResult> specification)
    {
        return SpecificationEvaluator.GetQuery(DbContext.Set<T>().AsQueryable(), specification);
    }
}
