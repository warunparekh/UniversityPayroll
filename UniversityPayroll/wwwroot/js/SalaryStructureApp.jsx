const defaultStructure = () => ({
    designation: '',
    allowances: { daPercent: 0, hraPercent: 0 },
    annualIncrementPercent: 0,
    pf: { employeePercent: 0, employerPercent: 0, edliPercent: 0 }
});

function SalaryStructureApp() {
    const [structures, setStructures] = React.useState(window.structuresData);
    const [showForm, setShowForm] = React.useState(false);
    const [editingId, setEditingId] = React.useState(null);
    const [formData, setFormData] = React.useState(defaultStructure());

    const resetForm = () => { setFormData(defaultStructure()); setEditingId(null); };

    const handleSubmit = async (ev) => {
        ev.preventDefault();
        const url = editingId ? '/SalaryStructure/EditAjax' : '/SalaryStructure/CreateAjax';
        await fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ ...formData, id: editingId })
        });
        window.location.reload();
    };

    const updateField = (field, value) => {
        setFormData({ ...formData, [field]: value });
    };

    const updateNested = (group, field, value) => {
        setFormData({
            ...formData,
            [group]: { ...formData[group], [field]: Number(value) || 0 }
        });
    };

    const showEditForm = async (id) => {
        const data = await fetch(`/SalaryStructure/GetById?id=${id}`).then(r => r.json());
        setFormData(data);
        setEditingId(id);
        setShowForm(true);
    };

    const deleteStructure = async (id) => {
        if (!confirm('Delete this structure?')) return;
        await fetch('/SalaryStructure/DeleteAjax', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(id)
        });
        setStructures(structures.filter(s => s.id !== id));
    };

    const renderForm = () => (
        <div className="container mt-3">
            <h2>{editingId ? 'Edit Structure' : 'New Structure'}</h2>
            <form onSubmit={handleSubmit}>
                <input type="text" className="form-control mb-2" placeholder="Designation" value={formData.designation} onChange={e => updateField('designation', e.target.value)} required />
                <input type="number" className="form-control mb-2" placeholder="DA %" value={formData.allowances.daPercent} onChange={e => updateNested('allowances', 'daPercent', e.target.value)} required />
                <input type="number" className="form-control mb-2" placeholder="HRA %" value={formData.allowances.hraPercent} onChange={e => updateNested('allowances', 'hraPercent', e.target.value)} required />
                <input type="number" className="form-control mb-2" placeholder="Increment %" value={formData.annualIncrementPercent} onChange={e => updateField('annualIncrementPercent', Number(e.target.value))} required />
                <input type="number" className="form-control mb-2" placeholder="PF Emp %" value={formData.pf.employeePercent} onChange={e => updateNested('pf', 'employeePercent', e.target.value)} required />
                <input type="number" className="form-control mb-2" placeholder="PF Empr %" value={formData.pf.employerPercent} onChange={e => updateNested('pf', 'employerPercent', e.target.value)} required />
                <input type="number" className="form-control mb-3" placeholder="EDLI %" value={formData.pf.edliPercent} onChange={e => updateNested('pf', 'edliPercent', e.target.value)} required />
                <button type="submit" className="btn btn-primary me-2">{editingId ? 'Update' : 'Save'}</button>
                <button type="button" className="btn btn-secondary" onClick={() => setShowForm(false)}>Cancel</button>
            </form>
        </div>
    );

    const renderTable = () => (
        <div className="container mt-3">
            <h2>Salary Structures</h2>
            <button className="btn btn-primary mb-3" onClick={() => { resetForm(); setShowForm(true); }}>New Structure</button>
            <table className="table table-striped">
                <thead>
                    <tr>
                        <th>Designation</th><th>DA %</th><th>HRA %</th><th>Increment %</th><th>PF Emp %</th><th>PF Empr %</th><th>EDLI %</th><th>Actions</th>
                    </tr>
                </thead>
                <tbody>
                    {structures.map(s => (
                        <tr key={s.id}>
                            <td>{s.designation}</td>
                            <td>{s.allowances.daPercent}</td>
                            <td>{s.allowances.hraPercent}</td>
                            <td>{s.annualIncrementPercent}</td>
                            <td>{s.pf.employeePercent}</td>
                            <td>{s.pf.employerPercent}</td>
                            <td>{s.pf.edliPercent}</td>
                            <td>
                                <button className="btn btn-sm btn-secondary me-1" onClick={() => showEditForm(s.id)}>Edit</button>
                                <button className="btn btn-sm btn-danger" onClick={() => deleteStructure(s.id)}>Delete</button>
                            </td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );

    return showForm ? renderForm() : renderTable();
}

ReactDOM.createRoot(document.getElementById('react-root')).render(<SalaryStructureApp />);
