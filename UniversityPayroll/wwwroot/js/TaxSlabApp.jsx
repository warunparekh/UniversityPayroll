const defaultSlabs = () => Array(6).fill().map(() => ({ from: 0, to: null, rate: 0 }));
const defaultFormData = () => ({ financialYear: '', cessPercent: 0, slabs: defaultSlabs() });

function TaxSlabApp() {
  const [taxSlabs, setTaxSlabs] = React.useState(window.taxSlabsData);
  const [showForm, setShowForm] = React.useState(false);
  const [editingId, setEditingId] = React.useState(null);
  const [formData, setFormData] = React.useState(defaultFormData());

  const resetForm = () => {
    setFormData(defaultFormData());
    setEditingId(null);
  };

  const handleSubmit = async ev => {
    ev.preventDefault();
    const url = editingId ? '/TaxSlab/EditAjax' : '/TaxSlab/CreateAjax';
    await fetch(url, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ ...formData, slabs: formData.slabs.filter(s => s.rate > 0), id: editingId })
    });
    window.location.reload();
  };

  const updateField = (field, value) => setFormData({ ...formData, [field]: value });

  const updateSlab = (i, field, val) => {
    const slabs = formData.slabs.slice();
    slabs[i] = { ...slabs[i], [field]: val === '' ? null : Number(val) || 0 };
    setFormData({ ...formData, slabs });
  };

  const showNewForm = () => {
    resetForm();
    setShowForm(true);
  };

  const showEditForm = async id => {
    const res = await fetch(`/TaxSlab/GetById?id=${id}`);
    const data = await res.json();
    setFormData({
      financialYear: data.financialYear,
      cessPercent: data.cessPercent,
      slabs: data.slabs.concat(defaultSlabs().slice(data.slabs.length))
    });
    setEditingId(id);
    setShowForm(true);
  };

  const deleteTaxSlab = async id => {
    if (!confirm('Delete?')) return;
    await fetch('/TaxSlab/DeleteAjax', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(id)
    });
    setTaxSlabs(taxSlabs.filter(t => t.id !== id));
  };

  const renderForm = () => (
    <div className="container mt-3">
      <h2>{editingId ? 'Edit Tax Slab' : 'New Tax Slab'}</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="text"
          className="form-control mb-2"
          placeholder="Financial Year"
          value={formData.financialYear}
          onChange={e => updateField('financialYear', e.target.value)}
          required
        />
        <input
          type="number"
          className="form-control mb-3"
          placeholder="Cess %"
          value={formData.cessPercent}
          onChange={e => updateField('cessPercent', Number(e.target.value))}
          required
        />
        <table className="table table-bordered">
          <thead>
            <tr><th>From</th><th>To</th><th>Rate</th></tr>
          </thead>
          <tbody>
            {formData.slabs.map((slab, i) => (
              <tr key={i}>
                <td><input type="number" className="form-control" value={slab.from || ''} onChange={e => updateSlab(i, 'from', e.target.value)} /></td>
                <td><input type="number" className="form-control" value={slab.to || ''} onChange={e => updateSlab(i, 'to', e.target.value)} /></td>
                <td><input type="number" className="form-control" value={slab.rate || ''} onChange={e => updateSlab(i, 'rate', e.target.value)} /></td>
              </tr>
            ))}
          </tbody>
        </table>
        <button type="submit" className="btn btn-primary me-2">{editingId ? 'Update' : 'Save'}</button>
        <button type="button" className="btn btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
      </form>
    </div>
  );

  const renderTable = () => (
    <div className="container mt-3">
      <h2>Tax Slabs</h2>
      <button className="btn btn-primary mb-3" onClick={showNewForm}>New Tax Slab</button>
      <table className="table table-striped">
        <thead>
          <tr><th>FY</th><th>Cess</th><th>Slabs</th><th>Actions</th></tr>
        </thead>
        <tbody>
          {taxSlabs.map(t => (
            <tr key={t.id}>
              <td>{t.financialYear}</td>
              <td>{t.cessPercent}</td>
              <td>{t.slabs.map(s => `${s.from}-${s.to || '∞'} @@ ${s.rate}%`).join('; ')}</td>
              <td>
                <button className="btn btn-sm btn-secondary me-1" onClick={() => showEditForm(t.id)}>Edit</button>
                <button className="btn btn-sm btn-danger" onClick={() => deleteTaxSlab(t.id)}>Delete</button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );

  return showForm ? renderForm() : renderTable();
}

ReactDOM.createRoot(document.getElementById('react-root')).render(<TaxSlabApp />);
