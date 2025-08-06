function initialStructure() {
    return {
        designation: '',
        allowances: { daPercent: 0, hraPercent: 0 },
        annualIncrementPercent: 0,
        pf: { employeePercent: 0, employerPercent: 0, edliPercent: 0 }
    };
}

function SalaryStructureApp() {
    var [structures, setStructures] = React.useState(window.structuresData);
    var [showForm, setShowForm] = React.useState(false);
    var [editingId, setEditingId] = React.useState(null);
    var [formData, setFormData] = React.useState(initialStructure());

    function resetForm() { 
        setFormData(initialStructure()); 
        setEditingId(null); 
    }

    function handleSubmit(ev) {
        ev.preventDefault();
        var url = editingId ? '/SalaryStructure/EditAjax' : '/SalaryStructure/CreateAjax';
        formData.id = editingId;
        fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(formData)
        }).then(function() {
            window.location.reload();
        });
    }

    function updateField(field, value) {
        formData[field] = value;
        setFormData(formData);
    }

    function updateNested(group, field, value) {
        formData[group][field] = Number(value) || 0;
        setFormData(formData);
    }

    function showEditForm(id) {
        fetch('/SalaryStructure/GetById?id=' + id).then(function(r) { return r.json(); }).then(function(data) {
            setFormData(data);
            setEditingId(id);
            setShowForm(true);
        });
    }

    function deleteStructure(id) {
        if (!confirm('Delete this structure?')) return;
        fetch('/SalaryStructure/DeleteAjax', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(id) });
        setStructures(structures.filter(function(s) { return s.id !== id; }));
    }

    function renderForm() {
        return (
            <div className="container mt-3">
                <h2>{editingId ? 'Edit Structure' : 'New Structure'}</h2>
                <form onSubmit={handleSubmit}>
                    <input type="text" className="form-control mb-2" placeholder="Designation" value={formData.designation} onChange={function(e) { updateField('designation', e.target.value); }} required />
                    <input type="number" className="form-control mb-2" placeholder="DA %" value={formData.allowances.daPercent} onChange={function(e) { updateNested('allowances', 'daPercent', e.target.value); }} required />
                    <input type="number" className="form-control mb-2" placeholder="HRA %" value={formData.allowances.hraPercent} onChange={function(e) { updateNested('allowances', 'hraPercent', e.target.value); }} required />
                    <input type="number" className="form-control mb-2" placeholder="Increment %" value={formData.annualIncrementPercent} onChange={function(e) { updateField('annualIncrementPercent', Number(e.target.value)); }} required />
                    <input type="number" className="form-control mb-2" placeholder="PF Emp %" value={formData.pf.employeePercent} onChange={function(e) { updateNested('pf', 'employeePercent', e.target.value); }} required />
                    <input type="number" className="form-control mb-2" placeholder="PF Empr %" value={formData.pf.employerPercent} onChange={function(e) { updateNested('pf', 'employerPercent', e.target.value); }} required />
                    <input type="number" className="form-control mb-3" placeholder="EDLI %" value={formData.pf.edliPercent} onChange={function(e) { updateNested('pf', 'edliPercent', e.target.value); }} required />
                    <button type="submit" className="btn btn-primary me-2">{editingId ? 'Update' : 'Save'}</button>
                    <button type="button" className="btn btn-secondary" onClick={function() { setShowForm(false); }}>Cancel</button>
                </form>
            </div>
        );
    }

    function renderTable() {
        return (
            <div className="container mt-3">
                <h2>Salary Structures</h2>
                <button className="btn btn-primary mb-3" onClick={function() { resetForm(); setShowForm(true); }}>New Structure</button>
                <table className="table table-striped">
                    <thead>
                        <tr><th>Designation</th><th>DA %</th><th>HRA %</th><th>Increment %</th><th>PF Emp %</th><th>PF Empr %</th><th>EDLI %</th><th>Actions</th></tr>
                    </thead>
                    <tbody>
                        {structures.map(function(s) {
                            return (
                                <tr key={s.id}>
                                    <td>{s.designation}</td>
                                    <td>{s.allowances.daPercent}</td>
                                    <td>{s.allowances.hraPercent}</td>
                                    <td>{s.annualIncrementPercent}</td>
                                    <td>{s.pf.employeePercent}</td>
                                    <td>{s.pf.employerPercent}</td>
                                    <td>{s.pf.edliPercent}</td>
                                    <td>
                                        <button className="btn btn-sm btn-secondary me-1" onClick={function() { showEditForm(s.id); }}>Edit</button>
                                        <button className="btn btn-sm btn-danger" onClick={function() { deleteStructure(s.id); }}>Delete</button>
                                    </td>
                                </tr>
                            );
                        })}
                    </tbody>
                </table>
            </div>
        );
    }

    return showForm ? renderForm() : renderTable();
}

ReactDOM.createRoot(document.getElementById('react-root')).render(<SalaryStructureApp />);
